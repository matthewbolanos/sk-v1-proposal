

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

    public abstract List<Type> OutputTypes();

    public abstract List<string> Capabilities();

    public abstract Task<FunctionResult> GetModelResultAsync(string pluginName, string name, string prompt, Dictionary<object, BinaryFile>? files = default);
    public abstract Task<FunctionResult> GetModelStreamingResultAsync(string pluginName, string name, string prompt, Dictionary<object, BinaryFile>? files = default);
}