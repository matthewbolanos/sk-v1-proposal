

using Azure;
using Azure.AI.OpenAI;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Orchestration;

namespace Microsoft.SemanticKernel.Handlebars;
public class OpenAIChatCompletion : AIService, IChatCompletion
{
    private readonly Connectors.AI.OpenAI.ChatCompletion.OpenAIChatCompletion azureChatCompletion;

    public OpenAIChatCompletion(string modelId, string apiKey): base(modelId)
    {
        this.azureChatCompletion = new Connectors.AI.OpenAI.ChatCompletion.OpenAIChatCompletion(
            modelId,
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