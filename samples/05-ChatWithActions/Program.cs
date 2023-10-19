
using Microsoft.SemanticKernel;
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

kernel.ImportFunctions(new Math(), "Math");

// Create a plan
var planner = new HandlebarsPlanner(kernel);
var plan = planner.CreatePlan("What is 5+(10*5)?", new List<string>(){"Math"});

Console.WriteLine("Plan:");
Console.WriteLine(plan);

// {{#set name="result1" value=(Math_Multiply number1=10 number2=5)}}
// {{#set name="result2" value=(Math_Add number1=5 number2=result1)}}
// {
//   "operations": [
//     {
//       "operation": "Multiply 10 by 5",
//       "result": {{json result1}}
//     },
//     {
//       "operation": "Add 5 to the result",
//       "result": {{json result2}}
//     }
//   ],
//   "result": {{json result2}},
//   "response": "The result of 5+(10*5) is {{result2}}"
// }

Console.WriteLine();

// Run the plan (Results are not likely to be correct because this sample is using mock functions)
var result = await plan.InvokeAsync(kernel, kernel.CreateNewContext(), new Dictionary<string, object>());

Console.WriteLine("Result:");
Console.WriteLine(result);

// Result:
// {
//   "operations": [
//     {
//       "operation": "Multiply 10 by 5",
//       "result": "50"
//     },
//     {
//       "operation": "Add 5 to the result",
//       "result": "55"
//     }
//   ],
//   "result": "55",
//   "response": "The result of 5+(10*5) is 55"
// }