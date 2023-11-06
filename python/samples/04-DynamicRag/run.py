import asyncio
import os
import sys

from semantic_kernel.utils.settings import azure_openai_settings_from_dot_env_as_dict

# to allow the strange structure and the import of the new pieces
sys.path.append(os.getcwd())
from python.src.connectors import (
    RESPONSE_OBJECT_KEY,
    AzureChatCompletion,
)
from python.src.kernel import newKernel as Kernel
from python.src.plugins import SemanticFunction, SKPlugin

sys.path.append(os.getcwd() + "/python/samples/04-DynamicRag/Plugins")
from MathPlugin.math import Math


async def runner():
    # create services and chat
    math_plugin = SKPlugin.from_class(
        "Math",
        Math(),
    )
    math_plugin.add_function(
        SemanticFunction(
            path=os.getcwd()
            + "/python/samples/04-DynamicRag/Plugins/MathPlugin/GenerateMathProblem.prompt.yaml"
        )
    )
    gpt35turbo = AzureChatCompletion(
        **azure_openai_settings_from_dot_env_as_dict(include_api_version=True),
    )
    intent_plugin = SKPlugin(
        "Intent",
        functions=[
            SemanticFunction(
                path=os.getcwd()
                + "/python/samples/04-DynamicRag/Plugins/IntentPlugin/GetIntent.prompt.yaml"
            )
        ],
    )

    chat_function = SemanticFunction(
        path=os.getcwd()
        + "/python/samples/04-DynamicRag/Plugins/ChatPlugin/GroundedChat.prompt.yaml"
    )
    # create kernel
    kernel = Kernel(ai_services=[gpt35turbo], plugins=[math_plugin, intent_plugin])

    # create chat_history
    chat_history = gpt35turbo.create_new_chat()

    # loop with input
    while True:
        user_input = input("User:> ")
        if user_input == "exit":
            break
        chat_history.add_user_message(user_input)

        # get response
        response = await kernel.run_async(
            chat_function,
            variables={
                "persona": "You are a snarky (yet helpful) teenage assistant. Make sure to use hip slang in every response.",
                "messages": chat_history,
            },
            service=gpt35turbo,
        )

        # print response
        print(f"Assistant:> {response[chat_function.output_variable_name]}")
        chat_history.add_openai_response(response[RESPONSE_OBJECT_KEY])


def __main__():
    asyncio.run(runner())


if __name__ == "__main__":
    __main__()