import asyncio
import os
import sys

from semantic_kernel.connectors.search_engine import BingConnector
from semantic_kernel.utils.settings import azure_openai_settings_from_dot_env_as_dict

# to allow the strange structure and the import of the new pieces
sys.path.append(os.getcwd())
from python.src.connectors import (
    AzureChatCompletion,
    StreamingResultToStdOutHook,
    TokenUsageHook,
)
from python.src.kernel import newKernel as Kernel
from python.src.plugins import SemanticFunction, SKPlugin

sys.path.append(os.getcwd() + "/python/samples/03-SimpleRag/Plugins")
from SearchPlugin.search import Search


async def runner():
    # create services and chat
    search_plugin = SKPlugin.from_class(
        "Search",
        Search(bing_connector=BingConnector(api_key=os.getenv("BING_API_KEY"))),
    )
    search_plugin.add_function(
        SemanticFunction.from_path(
            path=os.getcwd()
            + "/python/samples/03-SimpleRag/plugins/SearchPlugin/GetSearchQuery.prompt.yaml"
        )
    )
    gpt35turbo = AzureChatCompletion(
        **azure_openai_settings_from_dot_env_as_dict(include_api_version=True),
    )
    chat_function = SemanticFunction.from_path(
        path=os.getcwd()
        + "/python/samples/03-SimpleRag/plugins/ChatPlugin/GroundedChat.prompt.yaml"
    )
    # create kernel
    kernel = Kernel(
        ai_services=[gpt35turbo],
        plugins=[search_plugin],
        hooks=[StreamingResultToStdOutHook(), TokenUsageHook()],
    )

    # create chat_history
    chat_history = gpt35turbo.create_new_chat()

    # loop with input
    while True:
        user_input = input("User:> ")
        if user_input == "exit":
            break
        chat_history.add_user_message(user_input)

        stream = True
        # get response
        response = await kernel.run_async(
            chat_function,
            variables={
                "persona": "You are a snarky (yet helpful) teenage assistant. Make sure to use hip slang in every response.",
                "messages": chat_history,
            },
            request_settings={"stream": stream},
        )

        if not stream:
            # print response
            print(f"Assistant:> {response[chat_function.output_variable_name]}")
            print(f"Token usage: {response['token_usage']}")
        chat_history.add_assistant_message(response[chat_function.output_variable_name])


def __main__():
    asyncio.run(runner())


if __name__ == "__main__":
    __main__()
