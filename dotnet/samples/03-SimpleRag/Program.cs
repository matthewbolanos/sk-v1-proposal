
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Handlebars;
using ISKFunction = Microsoft.SemanticKernel.ISKFunction;
using IKernel = Microsoft.SemanticKernel.IKernel;

string Gpt35TurboDeploymentName = Env.Var("AzureOpenAI:Gpt35TurboDeploymentName")!;
string Gpt4DeploymentName = Env.Var("AzureOpenAI:Gpt4DeploymentName")!;
string AzureOpenAIEndpoint = Env.Var("AzureOpenAI:Endpoint")!;
string AzureOpenAIApiKey = Env.Var("AzureOpenAI:ApiKey")!;
string BingApiKey = Env.Var("Bing:ApiKey")!;
string currentDirectory = Directory.GetCurrentDirectory();

// Initialize the required functions and services for the kernel
ISKFunction chatFunction = SemanticFunction.GetFunctionFromYaml(currentDirectory + "/Plugins/ChatPlugin/GroundedChat.prompt.yaml");
IChatCompletion gpt35Turbo = new AzureOpenAIChatCompletion("gpt-3.5-turbo", AzureOpenAIEndpoint, AzureOpenAIApiKey, Gpt35TurboDeploymentName);
IChatCompletion gpt4 = new AzureOpenAIChatCompletion("gpt-4", AzureOpenAIEndpoint, AzureOpenAIApiKey, Gpt4DeploymentName);

// Create the search plugin
List<ISKFunction> searchPluginFunctions = NativeFunction.GetFunctionsFromObject(new Search(BingApiKey));
searchPluginFunctions.Add(SemanticFunction.GetFunctionFromYaml(currentDirectory + "/Plugins/SearchPlugin/GetSearchQuery.prompt.yaml"));
Plugin searchPlugin = new(
    "Search",
    functions: searchPluginFunctions
);

// Create new kernel
IKernel kernel = new Kernel(
    aiServices: new () { gpt35Turbo, gpt4 },
    plugins: new () { searchPlugin },
    promptTemplateEngines: new () {new HandlebarsPromptTemplateEngine()}
);

// Start the chat
ChatHistory chatHistory = gpt35Turbo.CreateNewChat();
while(true)
{
    Console.Write("User > ");
    chatHistory.AddUserMessage(Console.ReadLine()!);

    // Run the chat function
    // The grounded chat function uses the search plugin to perform a Bing search to ground the response
    // See Plugins/ChatPlugin/GroundedChat.prompt.yaml for the full prompt
    var result = await kernel.RunAsync(
        chatFunction,
        variables: new() {
            { "persona", "You are a snarky (yet helpful) teenage assistant. Make sure to use hip slang in every response." },
            { "messages", chatHistory }
        },
        streaming: true
    );
    
    Console.Write("Assistant > ");
    await foreach(var message in result.GetStreamingValue<string>()!)
    {
        Console.Write(message);
    }
    Console.WriteLine();
    chatHistory.AddAssistantMessage(await result.GetValueAsync<string>()!);
}