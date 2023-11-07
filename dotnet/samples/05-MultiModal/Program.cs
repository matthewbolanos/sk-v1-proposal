
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
string currentDirectory = Directory.GetCurrentDirectory();

// Initialize the required functions and services for the kernel
IChatCompletion gpt35Turbo = new AzureOpenAIChatCompletion("gpt-3.5-turbo", AzureOpenAIEndpoint, AzureOpenAIApiKey, Gpt35TurboDeploymentName);
HuggingFaceFillMaskTask huggingFaceFillMaskTask = new("bert-base-uncased", HuggingFaceApiKey, endpoint: huggingFaceFillMaskTaskEndpoint);
HuggingFaceQuestionAnsweringTask huggingFaceQuestionAnsweringTask = new("deepset/roberta-base-squad2", HuggingFaceApiKey, endpoint: huggingFaceQuestionAnsweringTaskEndpoint);
HuggingFaceSummarizationTask huggingFaceSummarizationTask = new("facebook/bart-large-cnn", HuggingFaceApiKey, endpoint: huggingFaceSummarizationTaskEndpoint);
HuggingFaceTextToImageTask huggingFaceTextToImageTask = new("runwayml/stable-diffusion-v1-5", HuggingFaceApiKey, endpoint: huggingFaceTextToImageTaskEndpoint);

// Create new kernel
IKernel kernel = new Kernel(
    aiServices: new () {
        gpt35Turbo,
        huggingFaceFillMaskTask,
        huggingFaceQuestionAnsweringTask,
        huggingFaceSummarizationTask,
        huggingFaceTextToImageTask
    },
    plugins: new () {  },
    promptTemplateEngines: new () {new HandlebarsPromptTemplateEngine()}
);

ISKFunction chatFunction = SemanticFunction.GetFunctionFromYaml(currentDirectory + "/Plugins/ChatPlugin/GroundedChat.prompt.yaml");
ISKFunction huggingFaceFillMaskTaskFunction = SemanticFunction.GetFunctionFromYaml(currentDirectory + "/Plugins/HuggingFace/FillMaskTask.prompt.yaml");
ISKFunction huggingFaceQuestionAnsweringTaskFunction = SemanticFunction.GetFunctionFromYaml(currentDirectory + "/Plugins/HuggingFace/QuestionAnsweringTask.prompt.yaml");
ISKFunction huggingFaceSummarizationTaskFunction = SemanticFunction.GetFunctionFromYaml(currentDirectory + "/Plugins/HuggingFace/SummarizationTask.prompt.yaml");
ISKFunction huggingFaceTextToImageTaskFunction = SemanticFunction.GetFunctionFromYaml(currentDirectory + "/Plugins/HuggingFace/TextToImageTask.prompt.yaml");

// Running Face Mask Task
var faceMaskTaskResult = await kernel.RunAsync( huggingFaceFillMaskTaskFunction, variables: new() {});
faceMaskTaskResult.TryGetMetadataValue<List<FillMaskTaskResponse>>(AIFunctionResultExtensions.ModelResultsMetadataKey, out var fillMaskTaskResponses);
PrintResult("Face Mask Task", faceMaskTaskResult.GetValue<string>()!, fillMaskTaskResponses);

// Running Summarization Task
var summarizationTaskResult = await kernel.RunAsync( huggingFaceSummarizationTaskFunction, variables: new() {});
summarizationTaskResult.TryGetMetadataValue<List<SummarizationTaskResponse>>(AIFunctionResultExtensions.ModelResultsMetadataKey, out var summarizationTaskResponses);
PrintResult("Face Mask Task", summarizationTaskResult.GetValue<string>()!, summarizationTaskResponses);

// Running Question Answering Task
var questionAnsweringTaskResult = await kernel.RunAsync( huggingFaceQuestionAnsweringTaskFunction, variables: new() {});
questionAnsweringTaskResult.TryGetMetadataValue<QuestionAnsweringTaskResponse>(AIFunctionResultExtensions.ModelResultsMetadataKey, out var questionAnsweringTaskResponses);
PrintResult("Question Answering Task", questionAnsweringTaskResult.GetValue<string>()!, questionAnsweringTaskResponses);

// Text to Image Task
var questionTextToImageResult = await kernel.RunAsync( huggingFaceTextToImageTaskFunction, variables: new() {});
Image image = questionTextToImageResult.GetValue<Image>()!;
var filePath = "/Users/matthewbolanos/Downloads/image.png";
await File.WriteAllBytesAsync(filePath, image.Bytes);
PrintResult("Text to Image", filePath, image.ToString());


static void PrintResult(string title, object result, object rawResult)
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