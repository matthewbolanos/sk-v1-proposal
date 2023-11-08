from typing import Any

from semantic_kernel.sk_pydantic import SKBaseModel

from . import NativeFunction, SKFunction


class SKPlugin(SKBaseModel):
    name: str
    description: str = ""
    functions: dict[str, SKFunction] = {}

    def __init__(self, name: str, functions: list["SKFunction"] | None = None):
        if functions:
            functions_dict = {func.name: func for func in functions}
        else:
            functions_dict = {}
        super().__init__(name=name, functions=functions_dict)

    @classmethod
    def from_class(cls, name: str, class_object: Any) -> "SKPlugin":
        functions = [
            NativeFunction(getattr(class_object, function))
            for function in dir(class_object)
            if not function.startswith("__")
            and hasattr(getattr(class_object, function), "__sk_function__")
            and getattr(class_object, function).__sk_function__
        ]
        return cls(name, functions)

    def add_function(self, function: SKFunction):
        self.functions[function.name] = function

    def add_function_from_class(self, class_object: Any):
        functions = [
            getattr(class_object, function)
            for function in dir(class_object)
            if not function.startswith("__")
            and hasattr(function, "__sk_function__")
            and function.__sk_function__
        ]
        self.functions.update(
            {function.name: NativeFunction(function) for function in functions}
        )

    @property
    def fqn_functions(self) -> dict[str, Any]:
        return {f"{self.name}_{name}": func for name, func in self.functions.items()}
