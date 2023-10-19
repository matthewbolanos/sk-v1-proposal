
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Handlebars;

string AzureOpenAIDeploymentName = Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!;
string AzureOpenAIEndpoint = Env.Var("AzureOpenAI:Endpoint")!;
string AzureOpenAIApiKey = Env.Var("AzureOpenAI:ApiKey")!;
string BingApiKey = Env.Var("Bing:ApiKey")!;

// Get the current directory
var currentDirectory = Directory.GetCurrentDirectory();

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
kernel.AddFunctions("Chat",
    HandlebarsAIFunction.FromYaml("Chat", currentDirectory + "/Plugins/ChatPlugin/GroundedChat.prompt.yaml"),
    HandlebarsAIFunction.FromYaml("Chat",currentDirectory + "/Plugins/ChatPlugin/GroundedChatComplete.prompt.yaml"),
    HandlebarsAIFunction.FromYaml("Chat",currentDirectory + "/Plugins/ChatPlugin/GetSearchQuery.prompt.yaml")
);
// TODO: This should use the AddFunctions() method
kernel.ImportFunctions(new Search(BingApiKey), "Search");

// Initialize a chat history
ChatHistory chatHistory = new();

while (true)
{
    Console.Write("User > ");
    chatHistory.AddUserMessage(Console.ReadLine()!);

    // Run the simple chat flow using multiple functions
    // var result = kernel.RunFlow(
    //     variables: new()
    //     {
    //         { "persona", "You are a snarky (yet helpful) teenage assistant. Make sure to use hip slang in every response." },
    //         { "messages", chatHistory }
    //     },
    //     "intent = Chat_GetSearchQuery(messages=messages)",
    //     "result = Search_Search(query=intent)",
    //     "Chat_GroundedChat(messages=messages, persona=persona, grounding=result)"
    // );

    // Run the simple chat flow from a single handlebars template
    var result = kernel.RunFlow(
        variables: new()
        {
            { "persona", "You are a snarky (yet helpful) teenage assistant. Make sure to use hip slang in every response." },
            { "messages", chatHistory }
        },
        "Chat_GroundedChatComplete(messages=messages, persona=persona)"
    );

    Console.WriteLine("Assistant > " + result);
    chatHistory.AddAssistantMessage(result);
}