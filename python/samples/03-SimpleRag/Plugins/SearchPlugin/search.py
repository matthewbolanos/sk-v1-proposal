from semantic_kernel import ContextVariables, SKContext
from semantic_kernel.connectors.search_engine import BingConnector
from semantic_kernel.core_skills import WebSearchEngineSkill
from semantic_kernel.memory.null_memory import NullMemory

from python.src.plugins import (
    sk_function,
    sk_function_parameter,
)


class Search:
    def __init__(self, bing_connector: BingConnector):
        self.web_skill = WebSearchEngineSkill(bing_connector)

    @sk_function(
        description="Performs a web search for a given query",
        name="Search",
    )
    @sk_function_parameter(
        name="query", description="The query to search for", required=True
    )
    @sk_function_parameter(
        name="num_results",
        description="The number of search results to return",
        default_value="1",
    )
    @sk_function_parameter(
        name="offset",
        description="The number of search results to skip",
        default_value="0",
    )
    @sk_function_parameter(
        name="search_result",
        direction="output",
        description="The search result",
        type="string",
        required=True,
    )
    async def search(self, variables, **kwargs):
        return await self.web_skill.search_async(
            query=variables["query"],
            context=SKContext(
                variables=ContextVariables(None, variables=variables),
                memory=NullMemory(),
                skill_collection=None,
            ),
        )
