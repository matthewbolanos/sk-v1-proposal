from abc import abstractmethod
from typing import Any

from semantic_kernel.sk_pydantic import SKBaseModel


class HookBase(SKBaseModel):
    name: str

    @abstractmethod
    async def __call__(self, *args: Any, **kwds: Any) -> Any:
        pass


class PostHookBase(HookBase):
    @abstractmethod
    async def __call__(
        self, results: list[dict[str, Any]], *args: Any, **kwds: Any
    ) -> list[dict[str, Any]]:
        pass
