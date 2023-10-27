

using Azure;
using Azure.AI.OpenAI;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;
using Microsoft.SemanticKernel.Orchestration;

public class AzureOpenAIChatCompletion : AIService, IChatCompletion
{
    private readonly AzureChatCompletion azureChatCompletion;

    public AzureOpenAIChatCompletion(string modelId, string endpoint, string apiKey, string deploymentName): base(modelId)
    {
        this.azureChatCompletion = new AzureChatCompletion(
            deploymentName,
            endpoint,
            apiKey
        );
    }

    public ChatHistory CreateNewChat(string? instructions = null)
    {
        return this.azureChatCompletion.CreateNewChat(instructions);
    }

    public Task<IReadOnlyList<IChatResult>> GetChatCompletionsAsync(ChatHistory chat, AIRequestSettings? requestSettings = null, CancellationToken cancellationToken = default)
    {
        return this.azureChatCompletion.GetChatCompletionsAsync(chat, requestSettings, cancellationToken);
    }

    public IAsyncEnumerable<IChatStreamingResult> GetStreamingChatCompletionsAsync(ChatHistory chat, AIRequestSettings? requestSettings = null, CancellationToken cancellationToken = default)
    {
        return this.azureChatCompletion.GetStreamingChatCompletionsAsync(chat, requestSettings, cancellationToken);
    }

    public override ModelResult GetModelResultAsync(string prompt)
    {
        throw new NotImplementedException();
    }

    public override ModelResult GetModelStreamingResultAsync(string prompt)
    {
        throw new NotImplementedException();
    }
}