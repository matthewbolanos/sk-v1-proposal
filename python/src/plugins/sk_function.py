from abc import abstractmethod

from semantic_kernel.sk_pydantic import SKBaseModel
from semantic_kernel.skill_definition.parameter_view import ParameterView as Parameter


class SKFunction(SKBaseModel):
    name: str
    description: str
    input_variables: list[Parameter]
    output_variables: list[Parameter]

    @abstractmethod
    async def run_async(self, variables, **kwargs) -> dict:
        pass

    @property
    def output_variable_name(self) -> str:
        return self.output_variables[0].name
