# imports


import os
import asyncio
# from semantic_kernel import Kernel
from semantic_kernel.utils.settings import azure_openai_settings_from_dot_env_as_dict
from src.sk_function import SKFunction
from src.azure_chat_completion import NewAzureChatCompletion as AzureChatCompletion

async def runner():
    # create services and chat
    gpt35turbo = AzureChatCompletion(
        **azure_openai_settings_from_dot_env_as_dict(include_api_version=True),
    )
    chat_function = SKFunction.from_yaml(
        os.getcwd() + "/python-samples/ChatPlugin/SimpleChat.prompt.yaml"
    )

    # create kernel
    # kernel = Kernel(ai_services=[gpt35turbo])

    # create chat_history
    chat_history = gpt35turbo.create_new_chat()
    chat_history.add_system_message("Hello! I am a robot.")
    chat_history.add_user_message("Hello! I am a human.")

    vars = {"model": gpt35turbo.name, "messages": chat_history}
    rendered = chat_function.render(vars)
    print(rendered)
    # print(chat_function.template({"model": gpt35turbo.name, "messages": chat_history}))

    # loop with input
    while True:
        user_input = input("User:> ")
        if user_input == "exit":
            break
        chat_history.add_user_message(user_input)

        # get response
        # response = await kernel.run_async(
        #     chat_function, 
        #     variables={"messages": chat_history},
        #     service=gpt35turbo
        # )

        # # print response
        # print(f"Assistant:> {response}")
        # chat_history.add_assistant_message(response)


if __name__ == "__main__":
    asyncio.run(runner())
