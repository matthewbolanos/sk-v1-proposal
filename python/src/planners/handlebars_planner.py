import os
from typing import Any

from pydantic import Field
from semantic_kernel.sk_pydantic import SKBaseModel

from ..kernel import newKernel
from ..plugins.semantic_function import SemanticFunction
from .handlebars_plan import HandleBarsPlan


class HandleBarsPlannerConfig(SKBaseModel):
    excluded_functions: list[str] = []
    excluded_plugins: list[str] = []
    included_functions: list[str] = []
    included_plugins: list[str] = []
    last_plan: str | None = None
    last_error: str | None = None


class HandleBarsPlanner(SKBaseModel):
    kernel: newKernel
    configuration: HandleBarsPlannerConfig = Field(
        default_factory=HandleBarsPlannerConfig
    )

    async def create_plan(
        self, goal: str, variables: dict[str, Any] | None = None
    ) -> HandleBarsPlan:
        # read template
        planner_function = SemanticFunction.from_path(
            path=os.getcwd() + "/python/src/planners/handlebar_planner.prompt.yaml"
        )
        functions = [
            function
            for plugin in self.kernel.plugins
            for func_name, function in plugin.fqn_functions.items()
            if self._should_include_function(plugin.name, func_name)
        ]
        plan = await self.kernel.run_async(
            planner_function,
            variables={
                "functions": functions,
                "goal": goal,
                "last_plan": self.configuration.last_plan,
                "last_error": self.configuration.last_error,
            },
            request_settings={"stream": False},
        )
        return HandleBarsPlan(kernel=self.kernel, template=plan["result"])

    def _should_include_function(self, plugin: str, function: str) -> bool:
        """Return True if the function will be included."""
        should_include = (
            len(self.configuration.included_plugins) == 0
            and len(self.configuration.included_functions) == 0
        )
        if plugin in self.configuration.included_plugins:
            should_include = True
        if function in self.configuration.included_functions:
            should_include = True
        if plugin in self.configuration.excluded_plugins:
            should_include = False
        if function in self.configuration.excluded_functions:
            should_include = False
        return should_include
