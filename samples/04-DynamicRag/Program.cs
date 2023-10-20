
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;
using Microsoft.SemanticKernel.Handlebars;

string AzureOpenAIDeploymentName = Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!;
string AzureOpenAIEndpoint = Env.Var("AzureOpenAI:Endpoint")!;
string AzureOpenAIApiKey = Env.Var("AzureOpenAI:ApiKey")!;

// Get the current directory
var currentDirectory = Directory.GetCurrentDirectory();

// Create chat plugin
Plugin chatPlugin = new Plugin(
    "Chat",
    new () {
        HandlebarsAIFunction.FromYaml("Chat", currentDirectory + "/Plugins/ChatPlugin/Chat.prompt.yaml"),
        HandlebarsAIFunction.FromYaml("Chat",currentDirectory + "/Plugins/ChatPlugin/GenerateMathProblem.prompt.yaml"),
        HandlebarsAIFunction.FromYaml("Chat",currentDirectory + "/Plugins/ChatPlugin/GetNextStep.prompt.yaml")
    }
);

// Initialize all necessary services outside of the kernel
IChatCompletion service = new AzureChatCompletion("gpt-35-turbo", AzureOpenAIEndpoint, AzureOpenAIApiKey);

// Create new kernel
IKernel kernel = new KernelBuilder()
    .WithAIService("gpt-35-turbo", service)
    .Build();

// TODO: These should be functions of the builder
kernel.AddPlugin(chatPlugin);
kernel.ImportFunctions(new Math(), "Math");
kernel.RegisterCustomFunction(SKFunction.FromNativeFunction(
    async (string math_problem) =>  {
        // Create a plan
        var planner = new HandlebarsPlanner(kernel);
        var plan = planner.CreatePlan(
            "Solve the following math problem.\n\n" + math_problem,
            new List<string>(){"Math"}
        );

        // Run the plan
        var result = await plan.InvokeAsync(kernel, kernel.CreateNewContext(), new Dictionary<string, object>());
        return result.GetValue<string>();
    },
    "Planner", "PerformMath",
    "Solves a math problem"
));

// Initialize a chat history
ChatHistory chatHistory = new();

while (true)
{
    Console.Write("User > ");
    chatHistory.AddUserMessage(Console.ReadLine()!);

    // Run the simple chat flow from a single handlebars template
    var result = kernel.RunAsync("Chat_Chat",
        variables: new()
        {
            { "messages", chatHistory }
        }
    );

    Console.WriteLine("Assistant > " + result);
    chatHistory.AddAssistantMessage(result);
}