import sys
from typing import Any, AsyncGenerator

from python.src.connectors.ai.openai.openai_chat_history import OpenAIChatHistory
from python.src.plugins.semantic_function import SemanticFunction
from python.src.plugins.sk_function import SKFunction

from ....hooks import HookBase
from .azure_chat_completion import RESPONSE_OBJECT_KEY


class StreamingResultToStdOutHook(HookBase):
    name: str = "streaming_result_to_stdout"
    result_key: str = "result"
    line_prefix: str = "Assistant:> "

    async def output_openai_object(
        self, openai_oject: AsyncGenerator
    ) -> dict[str, Any]:
        """
        Call the hook.

        :param result: The result to print.
        :return: The result to use.
        """
        full_content = ""
        async for res in openai_oject:
            for choice in res.choices:
                if "delta" not in choice:
                    continue
                if "role" in choice.delta:
                    sys.stdout.write(self.line_prefix)
                if "content" in choice.delta:
                    full_content += str(choice.delta.content)
                    sys.stdout.write(str(choice.delta.content))
        sys.stdout.write("\n")
        return full_content

    async def on_invoke_end(
        self,
        results: list[dict],
        functions: list[SKFunction],
        variables: dict[str, Any] | None = None,
        request_settings: dict[str, Any] | None = None,
        kwargs: dict | None = None,
    ) -> tuple[
        list[dict],
        list[SKFunction],
        dict[str, Any] | None,
        dict[str, Any] | None,
        dict | None,
    ]:
        for result in results:
            if RESPONSE_OBJECT_KEY in result:
                if not isinstance(result[RESPONSE_OBJECT_KEY], AsyncGenerator):
                    return results, functions, variables, request_settings, kwargs
            completion = await self.output_openai_object(result[RESPONSE_OBJECT_KEY])
            result[self.result_key] = completion
        return results, functions, variables, request_settings, kwargs


class AddAssistantMessageToHistoryHook(HookBase):
    name: str = "add_assistant_message_to_history"
    chat_history_variable_name: str = "messages"

    async def on_invoke_end(
        self,
        results: list[dict],
        functions: list[SKFunction],
        variables: dict[str, Any] | None = None,
        request_settings: dict[str, Any] | None = None,
        kwargs: dict | None = None,
    ) -> tuple[
        list[dict],
        list[SKFunction],
        dict[str, Any] | None,
        dict[str, Any] | None,
        dict | None,
    ]:
        for idx in range(0, len(results)):
            result = results[idx]
            function = functions[idx]
            if (
                isinstance(function, SemanticFunction)
                and function.output_variable_name in result
            ):
                response = result[function.output_variable_name]
                if self.chat_history_variable_name in variables:
                    chat_history = variables[self.chat_history_variable_name]
                    if isinstance(chat_history, OpenAIChatHistory):
                        chat_history.add_assistant_message(response)
                    results[idx][self.chat_history_variable_name] = chat_history
        return results, functions, variables, request_settings, kwargs


class TokenUsageHook(HookBase):
    name: str = "token_usage"

    async def on_invoke_end(
        self,
        results: list[dict],
        functions: list[SKFunction],
        variables: dict[str, Any] | None = None,
        request_settings: dict[str, Any] | None = None,
        kwargs: dict | None = None,
    ) -> tuple[
        list[dict],
        list[SKFunction],
        dict[str, Any] | None,
        dict[str, Any] | None,
        dict | None,
    ]:
        for result in results:
            if RESPONSE_OBJECT_KEY in result:
                full_res = result[RESPONSE_OBJECT_KEY]
                if isinstance(full_res, AsyncGenerator):
                    continue
                if "usage" in full_res:
                    result["token_usage"] = {
                        "prompt_tokens": full_res.usage.prompt_tokens,
                        "completion_tokens": full_res.usage.completion_tokens,
                        "total_tokens": full_res.usage.total_tokens,
                    }
        return results, variables, request_settings, kwargs
