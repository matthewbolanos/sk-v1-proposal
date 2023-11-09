import re
from typing import Any, Final

from semantic_kernel.connectors.ai.open_ai.models.chat.function_call import FunctionCall
from semantic_kernel.sk_pydantic import SKBaseModel

from .openai_chat_message import OpenAIChatMessage

RESPONSE_OBJECT_KEY: Final = "response_object"

USER_ROLE: Final = "user"
ASSISTANT_ROLE: Final = "assistant"
SYSTEM_ROLE: Final = "system"
FUNCTION_CALL_ROLE: Final = "function"
TOOL_ROLE: Final = "tool"
ALL_ROLES = [USER_ROLE, ASSISTANT_ROLE, SYSTEM_ROLE, FUNCTION_CALL_ROLE, TOOL_ROLE]


class OpenAIChatHistory(SKBaseModel):
    messages: list[OpenAIChatMessage] = []

    def add_user_message(self, message: str):
        self.add_message(role=USER_ROLE, content=message)

    def add_assistant_message(
        self, message: str | None = None, function_call: FunctionCall | None = None
    ):
        self.add_message(
            role=ASSISTANT_ROLE, content=message, function_call=function_call
        )

    def add_system_message(self, message: str):
        self.add_message(role=SYSTEM_ROLE, content=message)

    def add_function_call_response(self, function_name: str, result: str):
        self.add_message(role=FUNCTION_CALL_ROLE, content=result, name=function_name)

    def add_message(
        self,
        role: str,
        content: str | None,
        name: str | None = None,
        function_call: FunctionCall | None = None,
        tool_calls: list[FunctionCall] | None = None,
    ):
        if role not in ALL_ROLES:
            raise ValueError(f"Invalid role: {role}")
        if (function_call or tool_calls) and not role == "assistant":
            raise ValueError("Only assistant messages can have function or tool calls")
        if name and not role not in (FUNCTION_CALL_ROLE, TOOL_ROLE):
            raise ValueError("Only function calls and tools can have names")
        self.messages.append(
            OpenAIChatMessage(
                role=role,
                content=content,
                name=name,
                function_call=function_call,
                tool_calls=tool_calls,
            )
        )

    def add_openai_response(self, response: Any):
        if "choices" in response:
            message = response.choices[0].message
            self.add_message(**message)
        else:
            self.add_message(**response)

    def __iter__(self) -> iter:
        return self.messages.__iter__()

    def __len__(self) -> int:
        return len(self.messages)

    @classmethod
    def from_rendered_template(cls, rendered_template: str):
        # <message role="system">You are a helpful assistant.\n</message>\n<message role="user">how do handlebars work?</message>
        # TODO: decide on format of function/tool calls and name fields in intermediate template.
        chat_history = cls()
        [
            chat_history.add_message(role=match[0], content=match[1])
            for match in re.findall(
                r'<message role="(\w+)">(.*?)</message>',
                rendered_template.strip(),
                re.DOTALL,
            )
        ]
        return chat_history
