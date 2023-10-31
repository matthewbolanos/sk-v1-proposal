import re
from typing import Any

import yaml
from semantic_kernel.sk_pydantic import SKBaseModel
from semantic_kernel.skill_definition.parameter_view import ParameterView as Parameter

from python.src.handlebars_prompt_template_handler import (
    HandleBarsPromptTemplateHandler,
)


class SKFunction(SKBaseModel):
    name: str
    template: HandleBarsPromptTemplateHandler | str
    template_format: str
    description: str
    input_variables: list[Parameter]
    output_variables: list[Parameter]
    execution_settings: dict

    @classmethod
    def from_yaml(cls, path) -> "SKFunction":
        # read  the file
        # parse the yaml
        # create the function
        with open(path) as file:
            yaml_data = yaml.load(file, Loader=yaml.FullLoader)

        # parse the yaml
        template = yaml_data["template"]
        if yaml_data["template_format"] == "handlebars":
            template = HandleBarsPromptTemplateHandler(template)

        input_variables = [
            Parameter(
                name=variables.get("name"),
                description=variables.get("description"),
                default_value="",
                type=variables.get("type"),
                required=variables.get("is_required", False),
            )
            for variables in yaml_data["input_variables"]
        ]
        # parse output variables, just list for common interface with native functions.
        output_variables = [
            Parameter(
                name=yaml_data["output_variable"].get("name", "result"),
                description=yaml_data["output_variable"].get("description"),
                default_value="",
                type=yaml_data["output_variable"].get("type"),
                required=yaml_data["output_variable"].get("is_required", False),
            )
        ]
        # parse execution settings
        settings = {}
        for settings_dict in yaml_data["execution_settings"]:
            if "model_id_pattern" in settings_dict:
                model_pattern = re.compile(settings_dict["model_id_pattern"])
                del settings_dict["model_id_pattern"]
                settings.update({model_pattern: settings_dict})

        return SKFunction(
            name=yaml_data["name"],
            template=template,
            template_format=yaml_data["template_format"],
            description=yaml_data["description"],
            input_variables=input_variables,
            output_variables=output_variables,
            execution_settings=settings,
        )

    def render(self, variables: dict[str, Any]) -> str:
        return self.template.render(variables)

    @property
    def output_variable_name(self) -> str:
        return self.output_variables[0].name

    def settings_for_model(self, model_name: str) -> dict:
        for model_id in self.execution_settings.keys():
            if model_id.match(model_name):
                return self.execution_settings[model_id]

    async def run_async(self, variables, service, **kwargs) -> dict:
        return await service.complete_chat_async(
            self.template.render(variables),
            self.settings_for_model(service.name),
            self.output_variables,
            **kwargs,
        )
