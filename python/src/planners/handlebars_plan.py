import re

from ..connectors.ai.openai.azure_chat_completion import RESPONSE_OBJECT_KEY
from ..kernel import newKernel
from ..plugins import Parameter, SKFunction
from ..template_engine import HandleBarsPromptTemplateHandler


class HandleBarsPlan(SKFunction):
    name: str = "HandleBarsPlan"
    description: str = "HandleBarsPlan"
    input_variables: list[Parameter] = []
    output_variables: list[Parameter] = []
    kernel: newKernel
    template: str

    def __init__(self, kernel: newKernel, template: str):
        super().__init__(kernel=kernel, template=template)

    def __str__(self):
        return self.template

    async def run_async(self, variables, **kwargs) -> dict:
        matches = re.match(
            r".*(`{3}.?handlebars.?)(?P<template>.*)```.*",
            self.template,
            re.MULTILINE | re.S | re.IGNORECASE,
        )
        if matches:
            template = matches.group("template")
        else:
            return {
                self.output_variable_name: "Invalid HandleBars template",
                RESPONSE_OBJECT_KEY: None,
            }
        handle_bar_template = HandleBarsPromptTemplateHandler(template)
        return {
            self.output_variable_name: (
                await handle_bar_template.render(variables, **kwargs)
            ).strip(),
            RESPONSE_OBJECT_KEY: handle_bar_template,
        }
