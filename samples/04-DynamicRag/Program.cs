
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Handlebars;
using ISKFunction = Microsoft.SemanticKernel.ISKFunction;
using IKernel = Microsoft.SemanticKernel.IKernel;

string AzureOpenAIDeploymentName = Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!;
string AzureOpenAIEndpoint = Env.Var("AzureOpenAI:Endpoint")!;
string AzureOpenAIApiKey = Env.Var("AzureOpenAI:ApiKey")!;
var currentDirectory = Directory.GetCurrentDirectory();

// Initialize the required functions and services for the kernel
ISKFunction chatFunction = SemanticFunction.GetFunctionFromYaml(currentDirectory + "/Plugins/ChatPlugin/Chat.prompt.yaml");
IChatCompletion gpt35Turbo = new AzureOpenAIChatCompletion("gpt-3.5-turbo", AzureOpenAIEndpoint, AzureOpenAIApiKey, AzureOpenAIDeploymentName);

// Create intent plugin
Plugin intentPlugin = new(
    "Intent",
    functions: new () { SemanticFunction.GetFunctionFromYaml(currentDirectory + "/Plugins/IntentPlugin/GetNextStep.prompt.yaml") }
);

// Create math plugin
List<ISKFunction> mathFunctions = NativeFunction.GetFunctionsFromObject(new Math());
mathFunctions.Add(SemanticFunction.GetFunctionFromYaml(currentDirectory + "/Plugins/MathPlugin/GenerateMathProblem.prompt.yaml"));
Plugin mathPlugin = new(
    "Math",
    functions: mathFunctions
);

// Create new kernel
IKernel kernel = new Kernel(
    aiServices: new () { gpt35Turbo },
    plugins: new () { intentPlugin, mathPlugin },
    promptTemplateEngines: new () {new HandlebarsPromptTemplateEngine()},
    entryPoint: chatFunction
);

// Start the chat
ChatHistory chatHistory = new();
while (true)
{
    Console.Write("User > ");
    chatHistory.AddUserMessage(Console.ReadLine()!);

    // Run the chat function
    // The dynamic chat function uses a planner to create a plan that solves a math problem
    // See Plugins/MathPlugin/Math.cs for the code that runs the planner
    // See Plugins/ChatPlugin/GroundedChat.prompt.yaml for the full prompt
    var result = await kernel.RunAsync( new() {{ "messages", chatHistory }});

    Console.WriteLine("Assistant > " + result);
    chatHistory.AddAssistantMessage(result.GetValue<string>()!);
}