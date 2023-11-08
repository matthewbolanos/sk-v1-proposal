import asyncio
import json
import threading
from typing import Any

from pybars import Compiler
from pydantic import PrivateAttr
from semantic_kernel.sk_pydantic import SKBaseModel


def _message(this, options, **kwargs):
    # single message call, scope is messages object as context
    # in messages loop, scope is ChatMessage object as context
    if "role" in kwargs or "Role" in kwargs:
        role = kwargs.get("role" or "Role")
        if role:
            return f'<message role="{kwargs.get("role") or kwargs.get("Role")}">{options["fn"](this)}</message>'


def _set(this, *args, **kwargs):
    if "name" in kwargs and "value" in kwargs:
        this.context[kwargs["name"]] = kwargs["value"]
    return ""


def _array(this, *args, **kwargs):
    return {key: list(value) for key, value in kwargs.items()}


def _concat(this, *args, **kwargs):
    return "".join([str(value) for value in kwargs.values()])


def _equal(this, *args, **kwargs):
    return args[0] == args[1]


def _less_than(this, *args, **kwargs):
    return float(args[0]) < float(args[1])


def _greater_than(this, *args, **kwargs):
    return float(args[0]) > float(args[1])


def _less_than_or_equal(this, *args, **kwargs):
    return float(args[0]) <= float(args[1])


def _greater_than_or_equal(this, *args, **kwargs):
    return float(args[0]) >= float(args[1])


def _raw(this, options, *args, **kwargs):
    return options["fn"]()


def _json(this, *args, **kwargs):
    return json.dumps(args[0])


# TODO: render functions are helpers


class RunThread(threading.Thread):
    # TODO: replace with better solution and/or figure out why asyncio.run will not work, or move to handlebars implementation that van handle async
    def __init__(self, func, fixed_kwargs, args, kwargs):
        self.func = func
        self.args = args
        self.fixed_kwargs = fixed_kwargs
        self.kwargs = kwargs
        self.result = None
        super().__init__()

    def run(self):
        self.result = asyncio.run(self.func(variables=self.kwargs, **self.fixed_kwargs))


def create_func(function, fixed_kwargs):
    def func(context, *args, **kwargs):
        thread = RunThread(
            func=function.run_async, fixed_kwargs=fixed_kwargs, args=args, kwargs=kwargs
        )
        thread.start()
        thread.join()
        return thread.result

    return func


class HandleBarsPromptTemplateHandler(SKBaseModel):
    template: str
    _template_compiler: Any = PrivateAttr()

    def __init__(self, template: str):
        super().__init__(template=template)
        compiler = Compiler()
        self._template_compiler = compiler.compile(self.template)

    async def render(self, variables: dict, **kwargs) -> str:
        helpers = {
            "message": _message,
            "set": _set,
            "array": _array,
            "concat": _concat,
            "equal": _equal,
            "lessThan": _less_than,
            "greaterThan": _greater_than,
            "lessThanOrEqual": _less_than_or_equal,
            "greaterThanOrEqual": _greater_than_or_equal,
            "raw": _raw,
            "json": _json,
        }
        kwargs["called_by_template"] = True
        if "plugin_functions" in kwargs:
            plugin_functions = kwargs.get("plugin_functions")
            if plugin_functions:
                helpers.update(
                    {
                        name: create_func(function, kwargs)
                        for name, function in plugin_functions.items()
                    }
                )
        return self._template_compiler(variables, helpers=helpers)
