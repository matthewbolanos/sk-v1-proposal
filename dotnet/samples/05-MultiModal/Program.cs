
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Handlebars;
using ISKFunction = Microsoft.SemanticKernel.ISKFunction;
using IKernel = Microsoft.SemanticKernel.IKernel;
using System.Text.Json;
using Microsoft.SemanticKernel.AI;

string Gpt35TurboDeploymentName = Env.Var("AzureOpenAI:Gpt35TurboDeploymentName")!;
string AzureOpenAIEndpoint = Env.Var("AzureOpenAI:Endpoint")!;
string AzureOpenAIApiKey = Env.Var("AzureOpenAI:ApiKey")!;
string HuggingFaceApiKey = Env.Var("HuggingFace:ApiKey")!;
string huggingFaceFillMaskTaskEndpoint = Env.Var("HuggingFace:FillMaskTaskEndpoint")!;
string huggingFaceQuestionAnsweringTaskEndpoint = Env.Var("HuggingFace:QuestionAnsweringTaskEndpoint")!;
string huggingFaceSummarizationTaskEndpoint = Env.Var("HuggingFace:SummarizationTaskEndpoint")!;
string huggingFaceTextToImageTaskEndpoint = Env.Var("HuggingFace:TextToImageTaskEndpoint")!;
string OpenAIApiKey = Env.Var("OpenAI:ApiKey")!;
string currentDirectory = Directory.GetCurrentDirectory();

// Initialize the required functions and services for the kernel
IChatCompletion gpt35Turbo = new AzureOpenAIChatCompletion("gpt-3.5-turbo", AzureOpenAIEndpoint, AzureOpenAIApiKey, Gpt35TurboDeploymentName);
IChatCompletion gpt4vision = new OpenAIChatCompletion("gpt-4-vision-preview", OpenAIApiKey);
HuggingFaceFillMaskTask huggingFaceFillMaskTask = new("bert-base-uncased", HuggingFaceApiKey, endpoint: huggingFaceFillMaskTaskEndpoint);
HuggingFaceQuestionAnsweringTask huggingFaceQuestionAnsweringTask = new("deepset/roberta-base-squad2", HuggingFaceApiKey, endpoint: huggingFaceQuestionAnsweringTaskEndpoint);
HuggingFaceSummarizationTask huggingFaceSummarizationTask = new("facebook/bart-large-cnn", HuggingFaceApiKey, endpoint: huggingFaceSummarizationTaskEndpoint);
HuggingFaceTextToImageTask huggingFaceTextToImageTask = new("runwayml/stable-diffusion-v1-5", HuggingFaceApiKey, endpoint: huggingFaceTextToImageTaskEndpoint);
OllamaGeneration ollamaGeneration = new("wizard-math");

ISKFunction fillMaskTaskFunction = SemanticFunction.GetFunctionFromYaml(currentDirectory + "/Plugins/HuggingFace/FillMaskTask.prompt.yaml");
ISKFunction questionAnsweringTaskFunction = SemanticFunction.GetFunctionFromYaml(currentDirectory + "/Plugins/HuggingFace/QuestionAnsweringTask.prompt.yaml");
ISKFunction summarizationTaskFunction = SemanticFunction.GetFunctionFromYaml(currentDirectory + "/Plugins/HuggingFace/SummarizationTask.prompt.yaml");
ISKFunction textToImageTaskFunction = SemanticFunction.GetFunctionFromYaml(currentDirectory + "/Plugins/HuggingFace/TextToImageTask.prompt.yaml");
ISKFunction imageToTextTaskFunction = SemanticFunction.GetFunctionFromYaml(currentDirectory + "/Plugins/HuggingFace/ImageToTextTask.prompt.yaml");
ISKFunction ollamaGenerationFunction = SemanticFunction.GetFunctionFromYaml(currentDirectory + "/Plugins/Ollama/Math.prompt.yaml");

// Create plugin
Plugin huggingFaceTaskPlugin = new Plugin(
    name: "HuggingFace",
    functions: new () {
        fillMaskTaskFunction,
        questionAnsweringTaskFunction,
        summarizationTaskFunction,
        textToImageTaskFunction
    }
);

Plugin ollamaGenerationPlugin = new Plugin(
    name: "Ollama",
    functions: new () {
        ollamaGenerationFunction
    }
);

// Create new kernel
IKernel kernel = new Kernel(
    aiServices: new () {
        // gpt35Turbo,
        // gpt4vision,
        // huggingFaceFillMaskTask,
        // huggingFaceQuestionAnsweringTask,
        // huggingFaceSummarizationTask,
        // huggingFaceTextToImageTask
        ollamaGeneration
    },
    plugins: new () { ollamaGenerationPlugin },
    promptTemplateEngines: new () {new HandlebarsPromptTemplateEngine()}
);


// // Running Face Mask Task
// var faceMaskTaskResult = await kernel.RunAsync( fillMaskTaskFunction, variables: new() {});
// faceMaskTaskResult.TryGetMetadataValue<List<FillMaskTaskResponse>>(AIFunctionResultExtensions.ModelResultsMetadataKey, out var fillMaskTaskResponses);
// PrintResult("Face Mask Task", faceMaskTaskResult.GetValue<string>()!, fillMaskTaskResponses);

// // Running Summarization Task
// var summarizationTaskResult = await kernel.RunAsync( questionAnsweringTaskFunction, variables: new() {});
// summarizationTaskResult.TryGetMetadataValue<List<SummarizationTaskResponse>>(AIFunctionResultExtensions.ModelResultsMetadataKey, out var summarizationTaskResponses);
// PrintResult("Summarization Task", summarizationTaskResult.GetValue<string>()!, summarizationTaskResponses);

// // Running Question Answering Task
// var questionAnsweringTaskResult = await kernel.RunAsync( summarizationTaskFunction, variables: new() {});
// questionAnsweringTaskResult.TryGetMetadataValue<QuestionAnsweringTaskResponse>(AIFunctionResultExtensions.ModelResultsMetadataKey, out var questionAnsweringTaskResponses);
// PrintResult("Question Answering Task", questionAnsweringTaskResult.GetValue<string>()!, questionAnsweringTaskResponses);

// // Running Text to Image Task
// var questionTextToImageResult = await kernel.RunAsync( textToImageTaskFunction, variables: new() {});
// Image image = questionTextToImageResult.GetValue<Image>()!;
// var filePath = "/Users/matthewbolanos/Downloads/image.png";
// await File.WriteAllBytesAsync(filePath, image.Bytes);
// PrintResult("Text to Image Task", filePath, image.ToString());

// // Running Image to Text Task
// var imageToTextResult = await kernel.RunAsync( imageToTextTaskFunction, variables: new() {});
// imageToTextResult.TryGetMetadataValue<OpenAIChatResponse>(AIFunctionResultExtensions.ModelResultsMetadataKey, out var imageToTextTaskResponses);
// PrintResult("Image to Text Task", imageToTextResult.GetValue<string>()!, imageToTextTaskResponses);

// Running local Ollama Generation
var mathResult = await kernel.RunAsync(ollamaGenerationFunction, variables: new() {});
mathResult.TryGetMetadataValue<OllamaResponseModel>(AIFunctionResultExtensions.ModelResultsMetadataKey, out var ollamaGenerationResponses);
PrintResult("Ollama Generation", mathResult.GetValue<string>()!, ollamaGenerationResponses);



static void PrintResult(string title, object result, object? rawResult)
{
    Console.WriteLine(title);
    Console.WriteLine("====================================");
    Console.Write("Result: ");
    Console.ForegroundColor = ConsoleColor.Blue;
    Console.WriteLine(result);
    Console.ResetColor();
    Console.Write("Raw Result: ");
    Console.ForegroundColor = ConsoleColor.Blue;
    Console.WriteLine(JsonSerializer.Serialize(rawResult, new JsonSerializerOptions { WriteIndented = true }));
    Console.ResetColor();
    Console.WriteLine();
}