
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Handlebars;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;

string AzureOpenAIDeploymentName = Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!;
string AzureOpenAIEndpoint = Env.Var("AzureOpenAI:Endpoint")!;
string AzureOpenAIApiKey = Env.Var("AzureOpenAI:ApiKey")!;

// Initialize all necessary functions outside of the kernel
var currentDirectory = Directory.GetCurrentDirectory();
HandlebarsAIFunction chatFunction = HandlebarsAIFunction.FromYaml("Chat", currentDirectory + "/Plugins/ChatPlugin/PersonaChat.prompt.yaml");

// Initialize all necessary services outside of the kernel
IChatCompletion service = new AzureChatCompletion("gpt-35-turbo", AzureOpenAIEndpoint, AzureOpenAIApiKey);

// Create new kernel
IKernel kernel = new KernelBuilder()
    .WithAIService("gpt-35-turbo", service)
    //.WithDefaultFunction(chatFunction)
    .Build();

// TODO: This should be a method of the KernelBuilder
kernel.AddFunction("Chat", chatFunction);

// Initialize a chat history
ChatHistory chatHistory = new();

while(true)
{
    Console.Write("User > ");
    chatHistory.AddUserMessage(Console.ReadLine()!);

    // Run the simple chat flow
    var result = kernel.RunAsync("Chat.PersonaChat",
        variables: new()
        {
            { "persona", "You are a snarky (yet helpful) teenage assistant. Make sure to use hip slang in every response." },
            { "messages", chatHistory }
        }
    );

    // TODO: This could be further simplified to just...
    // var result = kernel.RunAsync(variables: new() { {
    //    "messages", chatHistory,
    //    "persona", "You are a snarky (yet helpful) teenage assistant. Make sure to use hip slang in every response."
    // } });

    Console.WriteLine("Assistant > " + result);
    chatHistory.AddAssistantMessage(result);
}