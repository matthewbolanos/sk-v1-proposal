from semantic_kernel.connectors.ai.open_ai import AzureChatCompletion
from semantic_kernel.sk_pydantic import SKBaseModel
from semantic_kernel.connectors.ai.open_ai.models.chat.open_ai_chat_message import (
    OpenAIChatMessage,
)
from semantic_kernel.connectors.ai.open_ai.models.chat.function_call import FunctionCall


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
            OpenAIChatMessage(role="function_call", fixed_content=result, name=function_name)
        )

    def __iter__(self) -> iter:
        return self.messages.__iter__()


class NewAzureChatCompletion(AzureChatCompletion):

    def create_new_chat(self):
        return OpenAIChatHistory()

    @property
    def name(self):
        # TODO: replace with name that can be initiated and matched to yaml model names
        return self._model_id