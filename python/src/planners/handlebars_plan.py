from ..kernel import newKernel
from ..plugins import Parameter, SKFunction
from ..template_engine import HandleBarsPromptTemplateHandler


class HandleBarsPlan(SKFunction):
    name: str = "HandleBarsPlan"
    description: str = "HandleBarsPlan"
    input_variables: list[Parameter] = []
    output_variables: list[Parameter] = []
    kernel: newKernel
    template: HandleBarsPromptTemplateHandler

    def __init__(self, kernel: newKernel, template: str):
        try:
            template = HandleBarsPromptTemplateHandler(template)
        except Exception as exc:
            return exc
        super().__init__(kernel=kernel, template=template)

    def __str__(self):
        return self.template.template

    async def run_async(self, variables, **kwargs) -> dict:
        return await self.template.render(variables)
