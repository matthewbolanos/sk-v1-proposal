from typing import Any

from pybars import Compiler
from pydantic import PrivateAttr
from semantic_kernel.sk_pydantic import SKBaseModel


def _message(this, options, **kwargs):
    # single message call, scope is messages object as context
    # in messages loop, scope is ChatMessage object as context
    if "role" in kwargs:
        return f'<message role="{kwargs["role"]}">{options["fn"](this)}</message>'


# TODO: render functions are helpers


class HandleBarsPromptTemplateHandler(SKBaseModel):
    template: str
    _template_compiler: Any = PrivateAttr()

    def __init__(self, template: str):
        super().__init__(template=template)
        compiler = Compiler()
        self._template_compiler = compiler.compile(self.template)

    def render(self, variables: dict) -> str:
        return self._template_compiler(variables, helpers={"message": _message})
