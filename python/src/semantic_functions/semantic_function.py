import re

import yaml
from semantic_kernel.skill_definition.parameter_view import ParameterView as Parameter

from python.src.template_engine.handlebars_prompt_template_handler import (
    HandleBarsPromptTemplateHandler,
)

from ..orchestration.sk_function import SKFunction


class SemanticFunction(SKFunction):
    template: HandleBarsPromptTemplateHandler | str
    template_format: str
    execution_settings: dict

    def __init__(self, *, path):
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
                type=yaml_data["output_variable"].get("type", "string"),
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

        super().__init__(
            name=yaml_data["name"],
            description=yaml_data["description"],
            input_variables=input_variables,
            output_variables=output_variables,
            template=template,
            template_format=yaml_data["template_format"],
            execution_settings=settings,
        )

    @property
    def output_variable_name(self) -> str:
        return self.output_variables[0].name

    def _settings_for_model(self, model_name: str) -> dict:
        for model_id in self.execution_settings.keys():
            if model_id.match(model_name):
                return self.execution_settings[model_id]

    async def run_async(self, variables, **kwargs) -> dict:
        if "service" not in kwargs:
            raise ValueError('"service" argument is required for a semantic function')
        rendered = await self.template.render(variables, **kwargs)
        service = kwargs.get("service")
        result = await service.complete_chat_async(
            rendered,
            request_settings=self._settings_for_model(service.name),
            output_variables=self.output_variables,
            **kwargs.get("service_kwargs", {}),
        )
        if kwargs.get("called_by_template", False):
            return result[self.output_variable_name]
        return result
