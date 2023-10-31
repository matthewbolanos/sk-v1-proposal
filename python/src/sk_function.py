from typing import Any, Union, Callable
from jinja2 import Template
import yaml
from semantic_kernel.skill_definition.parameter_view import ParameterView as Parameter
from semantic_kernel.sk_pydantic import SKBaseModel
from pybars import Compiler
import re


def parse_jinja2(template):
    return Template(template)


def parse_handlebars(template):
    compiler = Compiler()
    compiler._helpers = get_helpers()
    template = compiler.compile(template)
    return template


def get_helpers():
    return {"message": _message, "each": _each}


def parse_template(template, template_format):
    if template_format == "handlebars":
        return parse_handlebars(template)
    elif template_format == "jinja2":
        return parse_jinja2(template)
    return template


def _message(this, options, **kwargs):
    if "role" in kwargs:
        return kwargs["role"]
    if "content" in kwargs:
        return kwargs["content"]
    if "function_call" in kwargs:
        return kwargs["function_call"]
    if "name" in kwargs:
        return kwargs["name"]
    return ""


def _each(this, options, messages):
    return [f"{options['fn'](message)}" for message in messages.messages]
    # result = []
    # for message in messages.messages:
    #     result.extend(options['fn'](message))
    # return result


class SKFunction(SKBaseModel):
    name: str
    template: Union[str, Template, Callable]
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
        template = parse_template(yaml_data["template"], yaml_data["template_format"])

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
                name=yaml_data["output_variables"].get("name", "result"),
                description=yaml_data["output_variables"].get("description"),
                default_value="",
                type=yaml_data["output_variables"].get("type"),
                required=yaml_data["output_variables"].get("is_required", False),
            )
        ]
        # parse execution settings
        settings = {}
        for settings_dict in yaml_data["execution_settings"]:
            if 'model_id_pattern' in settings_dict:
                model_pattern = re.compile(settings_dict['model_id_pattern'])
                del settings_dict['model_id_pattern']
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

    def render(self, input_vars: dict[str, Any]) -> str:
        if self.template_format == "handlebars":
            return self.template(input_vars, helpers=get_helpers())

    @property
    def output_variable_name(self) -> str:
        return self.output_variables[0].name

    def settings_for_model(self, model_name: str) -> dict:
        for model_id in self.execution_settings.keys():
            if model_id.match(model_name):
                return self.execution_settings[model_id]

    async def run_async(self, variables, service, **kwargs) -> dict:
        # TODO: replace with rendering the template
        if 'messages' in variables:
            chat_history = variables['messages']
        return await service.complete_chat_async(
            chat_history,
            self.settings_for_model(service.name),
            self.output_variables,
            **kwargs
        )
