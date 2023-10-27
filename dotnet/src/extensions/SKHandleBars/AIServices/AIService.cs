

using Azure;
using Azure.AI.OpenAI;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Services;

public abstract class AIService : IAIService
{
    public string ModelId { get; }

    public AIService(string modelId)
    {
        ModelId = modelId;
    }

    public abstract ModelResult GetModelResultAsync(string prompt);
    public abstract ModelResult GetModelStreamingResultAsync(string prompt);
}