from typing import Any

from semantic_kernel.sk_pydantic import SKBaseModel

from python.src.plugins.sk_function import SKFunction


class HookBase(SKBaseModel):
    name: str

    async def on_invoke_start(
        self,
        functions: list[SKFunction],
        variables: dict[str, Any] | None = None,
        request_settings: dict[str, Any] | None = None,
        kwargs: dict | None = None,
    ) -> tuple[
        list[SKFunction],
        dict[str, Any] | None,
        dict[str, Any] | None,
        dict | None,
    ]:
        return functions, variables, request_settings, kwargs

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
        return results, functions, variables, request_settings, kwargs
