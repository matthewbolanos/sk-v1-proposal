import sys
from typing import Any, AsyncGenerator

from ....hooks import PostHookBase
from .azure_chat_completion import RESPONSE_OBJECT_KEY


class StreamingResultToStdOutHook(PostHookBase):
    name: str = "streaming_result_to_stdout"
    result_key: str = "result"
    line_prefix: str = "Assistant:> "

    async def __call__(
        self, results: list[dict[str, Any]], *args: Any, **kwds: Any
    ) -> list[dict[str, Any]]:
        """
        Call the hook.

        :param result: The result to print.
        :return: The result to use.
        """
        for result in results:
            full_content = ""
            if RESPONSE_OBJECT_KEY in result:
                full_res = result[RESPONSE_OBJECT_KEY]
                if not isinstance(full_res, AsyncGenerator):
                    continue
                async for res in full_res:
                    for choice in res.choices:
                        if "delta" not in choice:
                            continue
                        if "role" in choice.delta:
                            sys.stdout.write(self.line_prefix)

                        if "content" in choice.delta:
                            full_content += str(choice.delta.content)
                            sys.stdout.write(str(choice.delta.content))
            result[self.result_key] = full_content
            sys.stdout.write("\n")
        return results


class TokenUsageHook(PostHookBase):
    name: str = "token_usage"

    async def __call__(
        self, results: list[dict[str, Any]], *args: Any, **kwargs: Any
    ) -> list[dict[str, Any]]:
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
        return results
