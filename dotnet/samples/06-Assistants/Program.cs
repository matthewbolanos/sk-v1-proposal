
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Handlebars;
using ISKFunction = Microsoft.SemanticKernel.ISKFunction;
using IKernel = Microsoft.SemanticKernel.IKernel;
using System.Text.Json;
using Microsoft.SemanticKernel.AI;

string OpenAIApiKey = Env.Var("OpenAI:ApiKey")!;
string BingApiKey = Env.Var("Bing:ApiKey")!;
string currentDirectory = Directory.GetCurrentDirectory();

// Initialize the required functions and services for the kernel
IChatCompletion gpt35Turbo = new OpenAIChatCompletion("gpt-4-1106-preview", OpenAIApiKey);
IChatCompletion gpt4Vision = new OpenAIChatCompletion("gpt-4-vision-preview", OpenAIApiKey);

// Create plugins
IPlugin mathPlugin = new Plugin(
    name: "Math",
    functions: NativeFunction.GetFunctionsFromObject(new Math())
);

IPlugin searchPlugin = new Plugin(
    name: "Search",
    functions: NativeFunction.GetFunctionsFromObject(new Search(BingApiKey))
);


// Create a researcher
IPlugin researcher = AssistantKernel.FromConfiguration(
    currentDirectory + "/Assistants/Researcher.agent.yaml",
    aiServices: new () { gpt35Turbo },
    plugins: new () { searchPlugin }
);

// Create a mathmatician
IPlugin mathmatician = AssistantKernel.FromConfiguration(
    currentDirectory + "/Assistants/Mathmatician.agent.yaml",
    aiServices: new () { gpt4Vision },
    plugins: new () { mathPlugin }
);

// Create a designer
IPlugin designer = AssistantKernel.FromConfiguration(
    currentDirectory + "/Assistants/Designer.agent.yaml",
    aiServices: new () { gpt4Vision }
);

// Create a Project Manager
AssistantKernel projectManager = AssistantKernel.FromConfiguration(
    currentDirectory + "/Assistants/ProjectManager.agent.yaml",
    aiServices: new () { gpt35Turbo },
    plugins: new () { researcher, mathmatician, designer }
);

IThread thread = await projectManager.CreateThreadAsync();
while(true) {
    // Get user input
    Console.Write("User > ");
    thread.AddUserMessageAsync(Console.ReadLine());

    // Run the thread using the project manager kernel
    var result = await projectManager.RunAsync(thread);

    // Print the results
    var messages = result.GetValue<List<ModelMessage>>();
    foreach(ModelMessage message in messages)
    {
        Console.WriteLine("Project Manager > " + message);
    }
}