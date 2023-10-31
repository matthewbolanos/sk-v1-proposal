from typing import Any, Final
import openai
from semantic_kernel.sk_pydantic import SKBaseModel
from semantic_kernel.connectors.ai.open_ai.models.chat.open_ai_chat_message import (
    OpenAIChatMessage,
)
from semantic_kernel.skill_definition.parameter_view import ParameterView as Parameter
from semantic_kernel.connectors.ai.open_ai.models.chat.function_call import FunctionCall
from semantic_kernel.connectors.ai import ChatCompletionClientBase

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
        if 'choices' in response:
            message = response.choices[0].message
            self.messages.append(OpenAIChatMessage(**message))
        else:
            self.messages.append(OpenAIChatMessage(**response))

    def __iter__(self) -> iter:
        return self.messages.__iter__()


class AzureChatCompletion(SKBaseModel, ChatCompletionClientBase):
    deployment_name: str
    api_key: str
    endpoint: str
    api_version: str
    api_type: str = "azure"

    def create_new_chat(self):
        return OpenAIChatHistory()

    async def complete_chat_async(
        self,
        chat_history: OpenAIChatHistory,
        request_settings: dict,
        output_variables: list[Parameter] = None,
    ) -> dict:
        request_settings['stream'] = False
        response = await self._send_chat_request(
            chat_history, request_settings, None
        )
        result_key = output_variables[0].name if output_variables else 'result'
        res = {result_key: response.choices[0].message.content, RESPONSE_OBJECT_KEY: response}
        return res

    async def complete_chat_stream_async(
        self,
        chat_history: OpenAIChatHistory,
        request_settings: dict,
    ) -> dict:
        request_settings['stream'] = True
        response = await self._send_chat_request(
            chat_history, request_settings, None
        )
        return {RESPONSE_OBJECT_KEY: response}
    
    async def complete_chat_with_functions_async(
        self,
        chat_history: OpenAIChatHistory,
        functions: list[dict],
        request_settings: dict,
    ) -> dict:
        request_settings['stream'] = False
        response = await self._send_chat_request(
            chat_history, request_settings, functions
        )
        return {"result": response.choices[0].message, RESPONSE_OBJECT_KEY: response}

    async def _send_chat_request(
        self,
        chat_history: OpenAIChatHistory,
        request_settings: dict,
        functions: list[dict] = None,
    ):
        messages = [message.as_dict() for message in chat_history]

        model_args = {
            "api_key": self.api_key,
            "api_type": self.api_type,
            "api_base": self.endpoint,
            "api_version": self.api_version,
            "engine": self.deployment_name,
            "messages": messages,
            "temperature": request_settings.get("temperature", 0.0),
            "top_p": request_settings.get("top_p", 1.0),
            "n": request_settings.get("number_of_responses", 1),
            "stream": request_settings.get("stream", False),
            "max_tokens": request_settings.get("max_tokens", 256),
            "presence_penalty": request_settings.get("presence_penalty", 0.0),
            "frequency_penalty": request_settings.get("frequency_penalty", 0.0),
        }

        if functions and request_settings.get("function_call") is not None:
            model_args["function_call"] = request_settings["function_call"]
            model_args["functions"] = functions

        response: Any = await openai.ChatCompletion.acreate(**model_args)
        return response

    @property
    def name(self):
        # TODO: replace with name that can be initiated and matched to yaml model names
        return self.deployment_name
