

using Azure;
using Azure.AI.OpenAI;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Services;

namespace Microsoft.SemanticKernel.Handlebars;
public abstract class AIService : IAIService
{
    public string ModelId { get; }

    public AIService(string modelId)
    {
        ModelId = modelId;
    }

    // public abstract List<Type> MessageInputTypes(); // Text, Function, Image, Audio, Video, Doc

    public abstract List<Type> OutputTypes(); // Text, Function, Image, Audio, Video, Doc

    public abstract List<string> Capabilities(); // 

    public abstract Task<FunctionResult> GetModelResultAsync(IKernel kernel, string pluginName, string name, string prompt, Dictionary<object, BinaryFile>? files = default);
    public abstract Task<FunctionResult> GetModelStreamingResultAsync(IKernel kernel, string pluginName, string name, string prompt, Dictionary<object, BinaryFile>? files = default);
}