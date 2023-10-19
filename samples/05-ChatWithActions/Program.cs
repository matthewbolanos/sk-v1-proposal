
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

kernel.ImportFunctions(new Todo(), "Todo");

// Create a plan
var planner = new HandlebarsPlanner(kernel);
var plan = planner.CreatePlan("Delete all of the todo items that are about taking out the trash.");

Console.WriteLine("Plan:");
Console.WriteLine(plan);

// The plan should look something like the following
// [
//   {{#each (Todo_Search query="take out the trash") as |todo|}}
//     {{#with (Todo_Delete id=todo.id) as |deletedTodo|}}
//       {
//         "operation": "Delete todo item with ID {{todo.id}}",
//         "result": {{json deletedTodo}}
//       },
//     {{/with}}
//   {{/each}}
// ]

Console.WriteLine();

// Run the plan (Results are not likely to be correct because this sample is using mock functions)
var result = await plan.InvokeAsync(kernel, kernel.CreateNewContext(), new Dictionary<string, object>());


Console.WriteLine("Result:");
Console.WriteLine(result);