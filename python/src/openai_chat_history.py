import re
from typing import Any, Final

from semantic_kernel.connectors.ai.open_ai.models.chat.function_call import FunctionCall
from semantic_kernel.connectors.ai.open_ai.models.chat.open_ai_chat_message import (
    OpenAIChatMessage,
)
from semantic_kernel.sk_pydantic import SKBaseModel

RESPONSE_OBJECT_KEY: Final = "response_object"


class OpenAIChatHistory(SKBaseModel):
    messages: list[OpenAIChatMessage] = []

    def add_user_message(self, message: str):
        self.messages.append(OpenAIChatMessage(role="user", fixed_content=message))

    def add_assistant_message(
        self, message: str | None = None, function_call: FunctionCall | None = None
    ):
        self.messages.append(OpenAIChatMessage(role="assistant", fixed_content=message))

    def add_system_message(self, message: str):
        self.messages.append(OpenAIChatMessage(role="system", fixed_content=message))

    def add_function_call_response(self, function_name: str, result: str):
        self.messages.append(
            OpenAIChatMessage(
                role="function_call", fixed_content=result, name=function_name
            )
        )

    def add_openai_response(self, response: Any):
        if "choices" in response:
            message = response.choices[0].message
            self.messages.append(OpenAIChatMessage(**message))
        else:
            self.messages.append(OpenAIChatMessage(**response))

    def __iter__(self) -> iter:
        return self.messages.__iter__()

    def __len__(self) -> int:
        return len(self.messages)

    def __getitem__(self, key: int) -> Any:
        return self.messages[key]

    @classmethod
    def from_prompt(cls, rendered_template: str):
        # <message role="system">You are a helpful assistant.\n</message>\n<message role="user">how doe handlebars work?</message>

        chat_history = cls()
        rendered_template = rendered_template.strip()
        pattern = r'<message role="(\w+)">(.*?)</message>'
        matches = re.findall(pattern, rendered_template, re.DOTALL)
        for match in matches:
            role = match[0]
            content = match[1]
            if role == "user":
                chat_history.add_user_message(content)
            elif role == "system":
                chat_history.add_system_message(content)
            elif role == "assistant":
                chat_history.add_assistant_message(content)
            elif role == "function_call":
                chat_history.add_function_call_response(content)
            else:
                raise ValueError(f"Unknown role {role}")
        return chat_history
