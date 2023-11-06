import logging
from typing import Any

from semantic_kernel import Kernel
from semantic_kernel.connectors.ai import (
    ChatCompletionClientBase,
    EmbeddingGeneratorBase,
    TextCompletionClientBase,
)

from python.src.hooks.hook_base import HookBase
from python.src.plugins.semantic_function import SemanticFunction

from .plugins import SKFunction, SKPlugin

_LOGGER = logging.getLogger(__name__)


class newKernel(Kernel):
    plugins: list["SKPlugin"] = []
    services: list[
        ChatCompletionClientBase | TextCompletionClientBase | EmbeddingGeneratorBase
    ] = []
    prompt_template_engine: Any = None
    pre_hooks: list["HookBase"] = []
    post_hooks: list["HookBase"] = []

    def __init__(
        self,
        ai_services: list,
        plugins: list | None = None,
        prompt_template_engine: Any = None,
        pre_hooks: list["HookBase"] | None = None,
        post_hooks: list["HookBase"] | None = None,
        *args,
        **kwargs,
    ):
        super().__init__(*args, **kwargs)
        self.plugins = plugins
        self.services.extend(ai_services)
        self.prompt_template_engine = prompt_template_engine
        self.pre_hooks = pre_hooks or []
        self.post_hooks = post_hooks or []

    async def run_async(
        self,
        functions: list[SKFunction] | SKFunction,
        variables: dict[str, Any] | None = None,
        request_settings: dict[str, Any] | None = None,
        **kwargs: dict,
    ) -> dict:
        """
        Run the specified functions with the given arguments and return the results.

        :param functions: The functions to run.
        :param kwargs: The arguments to pass to the functions.
        :return: A dictionary of the results.
        """
        results = []
        if not isinstance(functions, list):
            functions = [functions]
        # TODO: apply pre-hooks
        for function in functions:
            if isinstance(function, SemanticFunction):
                results.append(
                    await function.run_async(
                        variables,
                        services=self.services,
                        request_settings=request_settings,
                        plugin_functions=self.fqn_functions,
                        **kwargs,
                    )
                )
                continue
            results.append(await function.run_async(variables, **kwargs))
        # TODO: apply post-hooks
        for hook in self.post_hooks:
            _LOGGER.info("Running hook: %s", hook.name)
            results = await hook(results, variables=variables)
        return results if len(results) > 1 else results[0]

    @property
    def fqn_functions(self) -> dict[str, Any] | None:
        if not self.plugins:
            return None
        all_fqn_functions = [plugin.fqn_functions for plugin in self.plugins]
        all = {}
        for funcs in all_fqn_functions:
            all.update(funcs)
        return all
