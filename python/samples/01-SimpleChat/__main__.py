import os
import sys
import asyncio

from semantic_kernel.utils.settings import azure_openai_settings_from_dot_env_as_dict
# to allow the strange structure and the import of the new pieces
sys.path.append(os.getcwd())
from python.src.kernel import newKernel as Kernel
from python.src.sk_function import SKFunction
from python.src.azure_chat_completion import AzureChatCompletion

async def runner():
    # create services and chat
    gpt35turbo = AzureChatCompletion(
        **azure_openai_settings_from_dot_env_as_dict(include_api_version=True),
    )
    chat_function = SKFunction.from_yaml(
        os.getcwd()
        + "/python/samples/01-SimpleChat/plugins/ChatPlugin/SimpleChat.prompt.yaml"
    )

    # create kernel
    kernel = Kernel(ai_services=[gpt35turbo])

    # create chat_history
    chat_history = gpt35turbo.create_new_chat()
    chat_history.add_system_message("Hello! I am a robot.")
    chat_history.add_user_message("Hello! I am a human.")
    # loop with input
    while True:
        user_input = input("User:> ")
        if user_input == "exit":
            break
        chat_history.add_user_message(user_input)

        # get response
        response = await kernel.run_async(
            chat_function,
            variables={"messages": chat_history},
            service=gpt35turbo
        )

        # print response
        print(f"Assistant:> {response['result']}")
        chat_history.add_openai_response(response['response_object'])

def __main__():
    asyncio.run(runner())

if __name__ == "__main__":
    __main__()
