from typing import Any, Union, Callable
from jinja2 import Template
from semantic_kernel import PromptTemplate, PromptTemplateConfig, SemanticFunctionConfig
import yaml
from semantic_kernel.skill_definition.parameter_view import ParameterView
from semantic_kernel.sk_pydantic import SKBaseModel
from semantic_kernel.template_engine.prompt_template_engine import PromptTemplateEngine
from pybars import Compiler

def parse_jinja2(template):
    return Template(template)

def parse_handlebars(template):
    compiler = Compiler()
    compiler._helpers = get_helpers()
    template = compiler.compile(template)
    return template

def get_helpers():
    return {
        "message": _message,
        "each": _each
    }

def parse_template(template, template_format):
    if template_format == "handlebars":
        return parse_handlebars(template)
    elif template_format == "jinja2":
        return parse_jinja2(template)
    return template

def _message(this, options, **kwargs):
    if 'role' in kwargs:
        return kwargs['role']
    if 'content' in kwargs:
        return kwargs['content']
    if 'function_call' in kwargs:
        return kwargs['function_call']
    if 'name' in kwargs:
        return kwargs['name']
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
    input_variables: list[ParameterView]
    output_variables: list[ParameterView]
    models: dict

    @classmethod
    def from_yaml(cls, path) -> "SKFunction":
        # read  the file
        # parse the yaml
        # create the function
        with open(path) as file:
            yaml_data = yaml.load(file, Loader=yaml.FullLoader)
        # parse the yaml

        template = parse_template(yaml_data["template"], yaml_data["template_format"])
        # if yaml_data["template_format"] == "handlebars":
        #     template = Template(yaml_data["template"])
        print(f"{template=}")

        input_variables = [
            ParameterView(
                name=variables.get("name"),
                description=variables.get("description"),
                default_value="",
                type=variables.get("type"),
                required=variables.get("is_required", False),
            )
            for variables in yaml_data["input_variables"]
        ]
        output_variables = [
            ParameterView(
                name=variables.get("name"),
                description=variables.get("description"),
                default_value="",
                type=variables.get("type"),
                required=variables.get("is_required", False),
            )
            for variables in yaml_data["output_variables"]
        ]

        return SKFunction(
            name=yaml_data["name"],
            template=template,
            template_format=yaml_data["template_format"],
            description=yaml_data["description"],
            input_variables=input_variables,
            output_variables=output_variables,
            models=yaml_data["models"],
        )

    @property
    def config(self) -> SemanticFunctionConfig:
        template_config = PromptTemplateConfig.from_completion_parameters(
            temperature=(
                self.models["gpt-4"].get("temperature", None)
                or self.models["gpt-3.5-turbo"].get("temperature", None)
                or self.models["default"].get("temperature", None)
                or 0.0
            ),
            max_tokens=(
                self.models["gpt-4"].get("max_tokens", None)
                or self.models["gpt-3.5-turbo"].get("max_tokens", None)
                or self.models["default"].get("max_tokens", None)
                or 2000
            ),
        )

        return SemanticFunctionConfig(
            prompt_template_config=template_config,
            prompt_template=PromptTemplate(
                self.template, PromptTemplateEngine(), template_config
            ),
        )

    def render(self, input_vars: dict[str, Any]) -> str:
        if self.template_format == 'handlebars':
            return self.template(input_vars, helpers=get_helpers())
