
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;

string AzureOpenAIDeploymentName = Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!;
string AzureOpenAIEndpoint = Env.Var("AzureOpenAI:Endpoint")!;
string AzureOpenAIApiKey = Env.Var("AzureOpenAI:ApiKey")!;

// Initialize all necessary functions
var currentDirectory = Directory.GetCurrentDirectory();
AIFunction chatFunction = AIFunction.FromYaml(currentDirectory + "/Plugins/ChatPlugin/SimpleChat.prompt.yaml");

// Create new kernel
IKernel kernel = new KernelBuilder()
    .WithAzureChatCompletionService(
        AzureOpenAIDeploymentName,  // The name of your deployment (e.g., "gpt-35-turbo")
        AzureOpenAIEndpoint,        // The endpoint of your Azure OpenAI service
        AzureOpenAIApiKey,          // The API key of your Azure OpenAI service
        serviceId: "gpt-35-turbo"   // The service ID of your Azure OpenAI service
    )
    .Build();

// TODO: AddFunctions() should be a method of the KernelBuilder
kernel.AddFunctions("Chat", chatFunction);

// Initialize a chat history
ChatHistory chatHistory = new();

while(true)
{
    Console.Write("User > ");
    chatHistory.AddUserMessage(Console.ReadLine()!);

    // Run the simple chat flow
    var result = await kernel.RunAsync(
        variables: new()
        {
            { "messages", chatHistory }
        },
        "SimpleChat(messages=messages)"
    );

    Console.WriteLine("Assistant > " + result);
    chatHistory.AddAssistantMessage(result);
}