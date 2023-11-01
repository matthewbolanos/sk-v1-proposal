import asyncio
import os

from semantic_kernel.connectors.search_engine import BingConnector
from semantic_kernel.core_skills import WebSearchEngineSkill


class SearchPlugin:
    def __init__(self, kernel):
        self.kernel = kernel
        connector = BingConnector(api_key=os.getenv("BING_API_KEY"))
        self.web_skill = self.kernel.import_skill(
            WebSearchEngineSkill(connector), "Search"
        )

    def search(self, variables):
        answer = asyncio.run(self.web_skill.search_async(query=variables["query"]))
        return answer
