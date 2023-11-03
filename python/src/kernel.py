from typing import Any

from semantic_kernel import Kernel
from semantic_kernel.connectors.ai import (
    ChatCompletionClientBase,
    EmbeddingGeneratorBase,
    TextCompletionClientBase,
)

from python.src.plugins.semantic_function import SemanticFunction

from .plugins import SKPlugin
from .semantic_functions import SKFunction


class newKernel(Kernel):
    plugins: list["SKPlugin"] = []
    prompt_template_engine: Any = None

    def __init__(
        self,
        ai_services: list,
        plugins: list | None = None,
        prompt_template_engine: Any = None,
        *args,
        **kwargs,
    ):
        super().__init__(*args, **kwargs)
        for service in ai_services:
            if isinstance(service, ChatCompletionClientBase):
                self.add_chat_service(service.name, service)
            if isinstance(service, TextCompletionClientBase):
                self.add_text_completion_service(service.name, service)
            if isinstance(service, EmbeddingGeneratorBase):
                self.add_text_embedding_generation_service(service.name, service)
        self.plugins = plugins
        self.prompt_template_engine = prompt_template_engine

    async def run_async(
        self,
        functions: list[SKFunction] | SKFunction,
        variables: dict[str, Any] | None = None,
        service: ChatCompletionClientBase
        | TextCompletionClientBase
        | EmbeddingGeneratorBase
        | None = None,
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
        for function in functions:
            if isinstance(function, SemanticFunction):
                results.append(
                    await function.run_async(
                        variables,
                        service=service,
                        plugin_functions=self.fqn_functions,
                        **kwargs,
                    )
                )
                continue
            results.append(await function.run_async(variables, **kwargs))
        # TODO: apply post-hooks
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
