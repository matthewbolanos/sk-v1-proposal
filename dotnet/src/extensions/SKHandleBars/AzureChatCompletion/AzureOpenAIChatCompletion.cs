

using Azure;
using Azure.AI.OpenAI;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;

public class AzureOpenAIChatCompletion : IChatCompletion
{
    private readonly AzureChatCompletion azureChatCompletion;
    public string ModelId { get; }

    public AzureOpenAIChatCompletion(string modelId, string endpoint, string apiKey, string? deploymentName = null)
    {
        ModelId = modelId;

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
}