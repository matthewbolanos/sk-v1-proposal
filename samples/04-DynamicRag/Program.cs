
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Handlebars;

string AzureOpenAIDeploymentName = Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!;
string AzureOpenAIEndpoint = Env.Var("AzureOpenAI:Endpoint")!;
string AzureOpenAIApiKey = Env.Var("AzureOpenAI:ApiKey")!;

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

kernel.AddFunctions("Chat",
    HandlebarsAIFunction.FromYaml("Chat", currentDirectory + "/Plugins/ChatPlugin/Chat.prompt.yaml"),
    HandlebarsAIFunction.FromYaml("Chat",currentDirectory + "/Plugins/ChatPlugin/GenerateMathProblem.prompt.yaml"),
    HandlebarsAIFunction.FromYaml("Chat",currentDirectory + "/Plugins/ChatPlugin/GetNextStep.prompt.yaml")
);
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
    var result = kernel.RunFlow(
        variables: new()
        {
            { "messages", chatHistory }
        },
        "Chat_Chat(messages=messages)"
    );

    Console.WriteLine("Assistant > " + result);
    chatHistory.AddAssistantMessage(result);
}