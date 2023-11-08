import logging
from typing import Any, Final

import openai
from semantic_kernel.connectors.ai import ChatCompletionClientBase
from semantic_kernel.sk_pydantic import SKBaseModel
from semantic_kernel.skill_definition.parameter_view import ParameterView as Parameter

from .openai_chat_history import OpenAIChatHistory

_LOGGER = logging.getLogger(__name__)

RESPONSE_OBJECT_KEY: Final = "response_object"


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
        rendered_template: str,
        *,
        request_settings: dict,
        output_variables: list[Parameter] = None,
        functions: list[dict] | None = None,
        **kwargs,
    ) -> dict:
        if "service" in kwargs:
            del kwargs["service"]
        chat_history = OpenAIChatHistory.from_rendered_template(rendered_template)
        response = await self._send_chat_request(
            chat_history, request_settings, functions=functions, **kwargs
        )

        if request_settings.get("stream", False):
            return {RESPONSE_OBJECT_KEY: response}
        result_key = output_variables[0].name if output_variables else "result"
        res = {
            result_key: response.choices[0].message.content,
            RESPONSE_OBJECT_KEY: response,
        }
        return res

    async def complete_chat_stream_async(
        self,
        rendered_template: str,
        request_settings: dict,
        output_variables: list[Parameter] = None,
        **kwargs,
    ) -> dict:
        _LOGGER.warning('Deprecated: use "complete_chat_async" instead')
        return await self.complete_chat_async(
            rendered_template, request_settings, output_variables, **kwargs
        )

    async def _send_chat_request(
        self,
        chat_history: OpenAIChatHistory,
        request_settings: dict,
        functions: list[dict] = None,
        **kwargs,
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

        if kwargs:
            model_args.update(kwargs)

        response: Any = await openai.ChatCompletion.acreate(**model_args)
        return response

    @property
    def name(self):
        # TODO: replace with name that can be initiated and matched to yaml model names
        return self.deployment_name
