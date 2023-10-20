
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;
using Microsoft.SemanticKernel.Handlebars;

string AzureOpenAIDeploymentName = Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!;
string AzureOpenAIEndpoint = Env.Var("AzureOpenAI:Endpoint")!;
string AzureOpenAIApiKey = Env.Var("AzureOpenAI:ApiKey")!;
string BingApiKey = Env.Var("Bing:ApiKey")!;

// Initialize all necessary functions outside of the kernel
var currentDirectory = Directory.GetCurrentDirectory();

// Create chat plugin
Plugin chatPlugin = new Plugin(
    "Chat",
    new () {
        HandlebarsAIFunction.FromYaml("Chat", currentDirectory + "/Plugins/ChatPlugin/GroundedChat.prompt.yaml"),
        HandlebarsAIFunction.FromYaml("Chat", currentDirectory + "/Plugins/ChatPlugin/GetSearchQuery.prompt.yaml")
    }
);

// Initialize all necessary services outside of the kernel
IChatCompletion service = new AzureChatCompletion("gpt-35-turbo", AzureOpenAIEndpoint, AzureOpenAIApiKey);

// Create new kernel
IKernel kernel = new KernelBuilder()
    .WithAIService("gpt-35-turbo", service)
    //.WithDefaultFunction(chatFunction)
    //.WithPlugin(chatPlugin)
    //.WithPlugin(searchPlugin)
    .Build();

// TODO: These should be functions of the builder
kernel.AddPlugin(chatPlugin);
kernel.ImportFunctions(new Search(BingApiKey), "Search");

// Initialize a chat history
ChatHistory chatHistory = new();

while (true)
{
    Console.Write("User > ");
    chatHistory.AddUserMessage(Console.ReadLine()!);

    // Run the simple chat flow from a single handlebars template
    var result = kernel.RunAsync("Chat.GroundedChat",
        variables: new()
        {
            { "persona", "You are a snarky (yet helpful) teenage assistant. Make sure to use hip slang in every response." },
            { "messages", chatHistory }
        }
    );

    Console.WriteLine("Assistant > " + result);
    chatHistory.AddAssistantMessage(result);
}