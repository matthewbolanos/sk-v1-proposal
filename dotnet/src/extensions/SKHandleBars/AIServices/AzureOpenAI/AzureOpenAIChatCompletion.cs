

using System.Runtime.CompilerServices;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;
using Microsoft.SemanticKernel.Orchestration;

namespace Microsoft.SemanticKernel.Handlebars;

public class AzureOpenAIChatCompletion : AIService, IChatCompletion
{
    private readonly AzureChatCompletion azureChatCompletion;
    private readonly ModelRequestXmlConverter modelRequestXmlConverter = new();

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

    public async Task<IReadOnlyList<IChatResult>> GetChatCompletionsAsync(ChatHistory chat, AIRequestSettings? requestSettings = null, CancellationToken cancellationToken = default)
    {
        return await this.azureChatCompletion.GetChatCompletionsAsync(chat, requestSettings, cancellationToken);
    }

    public IAsyncEnumerable<IChatStreamingResult> GetStreamingChatCompletionsAsync(ChatHistory chat, AIRequestSettings? requestSettings = null, CancellationToken cancellationToken = default)
    {
        return this.azureChatCompletion.GetStreamingChatCompletionsAsync(chat, requestSettings, cancellationToken);
    }

    public async override Task<FunctionResult> GetModelResultAsync(string pluginName, string name, string prompt)
    {
        ChatHistory chatHistory = this.ChatHistoryFromPrompt(prompt);

        var completionResults =  await this.GetChatCompletionsAsync(chatHistory).ConfigureAwait(false);
        var modelResults = completionResults.Select(c => c.ModelResult).ToArray();
        var result = new FunctionResult(name, pluginName, modelResults[0].GetOpenAIChatResult().Choice.Message.Content);
        result.Metadata.Add(AIFunctionResultExtensions.ModelResultsMetadataKey, modelResults);

        return result;
    }

    public async override Task<FunctionResult> GetModelStreamingResultAsync(string pluginName, string name, string prompt)
    {
        ChatHistory chatHistory = this.ChatHistoryFromPrompt(prompt);

        IAsyncEnumerable<IChatStreamingResult> completionResults = this.GetStreamingChatCompletionsAsync(chatHistory);
        return new FunctionResult(name, pluginName, ConvertToStrings(completionResults), ConvertToFinalStringAsync(completionResults));
    }
    
    private async IAsyncEnumerable<string> ConvertToStrings(IAsyncEnumerable<IChatStreamingResult> completionResults)
    {
        await foreach (var result in completionResults)
        {
            await foreach (var message in result.GetStreamingChatMessageAsync())
            {
                yield return message.Content;
            }
        }
    }

    private async Task<string> ConvertToFinalStringAsync(IAsyncEnumerable<IChatStreamingResult> completionResults)
    {
        string finalString = "";
        await foreach (var result in completionResults)
        {
            await foreach (var message in result.GetStreamingChatMessageAsync())
            {
                finalString += message.Content;
            }
        }

        return finalString;
    }

    private ChatHistory ChatHistoryFromPrompt(string prompt)
    {
        ModelRequest modelRequest = modelRequestXmlConverter.ParseXml(prompt);

        ChatHistory chatHistory = this.CreateNewChat();
        
        if (modelRequest.Context != null && modelRequest.Context.ContainsKey("context"))
        {
            chatHistory.AddSystemMessage(modelRequest.Context["context"].ToString()!);
        }

        foreach(ModelMessage modelMessage in modelRequest.Messages!)
        {
            AuthorRole authorRole = modelMessage.Role.ToLower() switch
            {
                "assistant" => AuthorRole.Assistant,
                "user" => AuthorRole.User,
                "system" => AuthorRole.System,
                "function" => AuthorRole.System,
                _ => throw new NotImplementedException()
            };
            if (modelMessage.Content is MessageParts contentArray)
            {
                chatHistory.AddMessage(authorRole, contentArray[0].ToString()!);
            }
        }
        return chatHistory;
    }
}