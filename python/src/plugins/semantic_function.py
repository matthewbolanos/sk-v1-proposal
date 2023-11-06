import re
from typing import Any

import yaml
from semantic_kernel.skill_definition.parameter_view import ParameterView as Parameter

from python.src.template_engine.handlebars_prompt_template_handler import (
    HandleBarsPromptTemplateHandler,
)

from .sk_function import SKFunction


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

    def _get_service_settings(
        self, service: Any, request_settings: dict[str, Any] | None = None
    ) -> dict:
        for model_id in self.execution_settings.keys():
            if model_id.match(service.name):
                settings = {
                    "request_settings": self.execution_settings[model_id],
                    "service": service,
                }
                if request_settings:
                    settings["request_settings"].update(request_settings)
                return settings

    def _get_service_and_settings(
        self, services: list, request_settings: dict[str, Any] | None = None
    ) -> dict:
        for svc in services:
            for model_id in self.execution_settings.keys():
                if model_id.match(svc.name):
                    settings = {
                        "request_settings": self.execution_settings[model_id],
                        "service": svc,
                    }
                    if request_settings:
                        settings["request_settings"].update(request_settings)
                    return settings

    async def run_async(
        self,
        variables,
        services=None,
        request_settings: dict[str, any] | None = None,
        **kwargs,
    ) -> dict:
        if "service" not in kwargs:
            service_settings = self._get_service_and_settings(
                services, request_settings
            )
            kwargs["service"] = service_settings["service"]
        else:
            service_settings = self._get_service_settings(
                kwargs["service"], request_settings
            )
        rendered = await self.template.render(variables, **kwargs)
        result = await service_settings["service"].complete_chat_async(
            rendered,
            request_settings=service_settings["request_settings"],
            output_variables=self.output_variables,
            **kwargs.get("service_kwargs", {}),
        )
        if kwargs.get("called_by_template", False):
            return result[self.output_variable_name]
        return result