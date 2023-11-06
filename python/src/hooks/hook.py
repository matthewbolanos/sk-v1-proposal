from typing import Any, Awaitable, Callable

from .hook_base import HookBase


class Hook(HookBase):
    func: Callable | Awaitable

    async def __call__(self, *args: Any, **kwds: Any) -> Any:
        if isinstance(self.func, Callable):
            return self.func(*args, **kwds)
        return await self.func(*args, **kwds)
