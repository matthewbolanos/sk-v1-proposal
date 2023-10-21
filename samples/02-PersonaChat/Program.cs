
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Handlebars;
using ISKFunction = Microsoft.SemanticKernel.ISKFunction;
using IKernel = Microsoft.SemanticKernel.IKernel;

string AzureOpenAIDeploymentName = Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!;
string AzureOpenAIEndpoint = Env.Var("AzureOpenAI:Endpoint")!;
string AzureOpenAIApiKey = Env.Var("AzureOpenAI:ApiKey")!;
string currentDirectory = Directory.GetCurrentDirectory();

ISKFunction chatFunction = SemanticFunction.GetFunctionFromYaml(currentDirectory + "/Plugins/ChatPlugin/PersonaChat.prompt.yaml");
IChatCompletion gpt35Turbo = new AzureOpenAIChatCompletion("gpt-3.5-turbo", AzureOpenAIEndpoint, AzureOpenAIApiKey, AzureOpenAIDeploymentName);

// Create new kernel
IKernel kernel = new Kernel(
    aiServices: new () { gpt35Turbo },
    promptTemplateEngines: new () {new HandlebarsPromptTemplateEngine()},
    entryPoint: chatFunction
);

// Start the chat
ChatHistory chatHistory = gpt35Turbo.CreateNewChat();
while(true)
{
    Console.Write("User > ");
    chatHistory.AddUserMessage(Console.ReadLine()!);

    // Run the simple chat flow
    var result = await kernel.RunAsync(variables: new() {
        { "persona", "You are a snarky (yet helpful) teenage assistant. Make sure to use hip slang in every response." },
        { "messages", chatHistory }
    });

    Console.WriteLine("Assistant > " + result);
    chatHistory.AddAssistantMessage(result.GetValue<string>()!);
}