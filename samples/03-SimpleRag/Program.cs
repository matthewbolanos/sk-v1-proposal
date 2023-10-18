
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;

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
    AIFunction.FromYaml(currentDirectory + "/Plugins/ChatPlugin/GroundedChat.prompt.yaml"),
    AIFunction.FromYaml(currentDirectory + "/Plugins/ChatPlugin/GroundedChatComplete.prompt.yaml"),
    AIFunction.FromYaml(currentDirectory + "/Plugins/ChatPlugin/GetSearchQuery.prompt.yaml")
);
// TODO: This should use the AddFunctions() method
kernel.ImportFunctions(new Search(BingApiKey), "_GLOBAL_FUNCTIONS_");

// Initialize a chat history
ChatHistory chatHistory = new();

while (true)
{
    Console.Write("User > ");
    chatHistory.AddUserMessage(Console.ReadLine()!);

    // Run the simple chat flow using multiple functions
    // var result = await kernel.RunAsync(
    //     variables: new()
    //     {
    //         { "persona", "You are a snarky (yet helpful) teenage assistant. Make sure to use hip slang in every response." },
    //         { "messages", chatHistory }
    //     },
    //     "intent = GetSearchQuery(messages=messages)",
    //     "result = Search(query=intent)",
    //     "GroundedChat(messages=messages, persona=persona, grounding=result)"
    // );

    // Run the simple chat flow from a single handlebars template
    var result = await kernel.RunAsync(
        variables: new()
        {
            { "persona", "You are a snarky (yet helpful) teenage assistant. Make sure to use hip slang in every response." },
            { "messages", chatHistory }
        },
        "GroundedChatComplete(messages=messages, persona=persona)"
    );

    Console.WriteLine("Assistant > " + result);
    chatHistory.AddAssistantMessage(result);
}