import asyncio
import os
import sys

from semantic_kernel.utils.settings import azure_openai_settings_from_dot_env_as_dict

# to allow the strange structure and the import of the new pieces
sys.path.append(os.getcwd())
from python.src.connectors import (
    AzureChatCompletion,
    StreamingResultToStdOutHook,
)
from python.src.kernel import newKernel as Kernel
from python.src.plugins import SemanticFunction, SKPlugin

sys.path.append(os.getcwd() + "/python/samples/04-DynamicRag/Plugins")
from MathPlugin.math import Math


async def runner():
    # create services and chat
    gpt35turbo = AzureChatCompletion(
        **azure_openai_settings_from_dot_env_as_dict(include_api_version=True),
    )
    gpt4 = AzureChatCompletion(
        deployment_name=os.getenv("AZURE_OPENAI_DEPLOYMENT_NAME_GPT4"),
        api_key=os.getenv("AZURE_OPENAI_API_KEY"),
        endpoint=os.getenv("AZURE_OPENAI_ENDPOINT"),
        api_version=os.getenv("AZURE_OPENAI_API_VERSION"),
    )
    math_plugin = SKPlugin.from_class(
        "Math",
        Math(),
    )
    math_plugin.add_function(
        SemanticFunction.from_path(
            path=os.getcwd()
            + "/python/samples/04-DynamicRag/Plugins/MathPlugin/GenerateMathProblem.prompt.yaml"
        )
    )
    intent_plugin = SKPlugin(
        "Intent",
        functions=[
            SemanticFunction.from_path(
                path=os.getcwd()
                + "/python/samples/04-DynamicRag/Plugins/IntentPlugin/GetNextStep.prompt.yaml"
            )
        ],
    )
    chat_function = SemanticFunction.from_path(
        path=os.getcwd()
        + "/python/samples/04-DynamicRag/Plugins/ChatPlugin/Chat.prompt.yaml"
    )
    # create kernel
    kernel = Kernel(
        ai_services=[gpt35turbo, gpt4],
        plugins=[math_plugin, intent_plugin],
        hooks=[StreamingResultToStdOutHook()],
    )

    # create chat_history
    chat_history = gpt4.create_new_chat()
    # loop with input
    while True:
        user_input = input("User:> ")
        if user_input == "exit":
            break
        chat_history.add_user_message(user_input)

        # get response
        results = await kernel.run_async(
            chat_function,
            variables={
                "persona": "You are a snarky (yet helpful) teenage assistant. Make sure to use hip slang in every response.",
                "messages": chat_history,
            },
            kernel=kernel,
        )
        chat_history.add_assistant_message(results["result"])


def __main__():
    asyncio.run(runner())


if __name__ == "__main__":
    __main__()
