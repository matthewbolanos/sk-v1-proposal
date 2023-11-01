import asyncio
import os

from semantic_kernel.connectors.search_engine import BingConnector
from semantic_kernel.core_skills import WebSearchEngineSkill
from semantic_kernel.skill_definition import sk_function, sk_function_context_parameter


class SearchPlugin:
    def __init__(self, kernel):
        self.kernel = kernel
        connector = BingConnector(api_key=os.getenv("BING_API_KEY"))
        self.web_skill = self.kernel.import_skill(
            WebSearchEngineSkill(connector), "Search"
        )

    @sk_function(description="Performs a web search for a given query", name="search")
    @sk_function_context_parameter(
        name="num_results",
        description="The number of search results to return",
        default_value="1",
    )
    @sk_function_context_parameter(
        name="offset",
        description="The number of search results to skip",
        default_value="0",
    )
    def search(self, variables):
        context = self.kernel.get_context()
        context.variables["num_results"] = variables["num_results"]
        context.variables["offset"] = variables["offset"]
        answer = asyncio.run(
            self.web_skill.search_async(query=variables["query"], context=context)
        )
        return answer
