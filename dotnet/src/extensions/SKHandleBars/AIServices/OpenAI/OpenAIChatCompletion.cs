

using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Azure.AI.OpenAI;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.ChatCompletion;

namespace Microsoft.SemanticKernel.Handlebars;
public class OpenAIChatCompletion : AIService, IChatCompletion
{
    private readonly Connectors.AI.OpenAI.ChatCompletion.OpenAIChatCompletion azureChatCompletion;
    private readonly ModelRequestXmlConverter modelRequestXmlConverter = new();

    private readonly HttpClient httpClient = new HttpClient();

    private const string endpoint = "https://api.openai.com/v1/chat/completions";

    internal string ApiKey;

    public OpenAIChatCompletion(string modelId, string apiKey): base(modelId)
    {
        this.ApiKey = apiKey;

        this.azureChatCompletion = new Connectors.AI.OpenAI.ChatCompletion.OpenAIChatCompletion(
            modelId,
            apiKey
        );

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
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

    public async override Task<FunctionResult> GetModelResultAsync(string pluginName, string name, string prompt, Dictionary<object, BinaryFile>? files = default)
    {
        StringContent chatHistory = this.ChatHistoryFromPrompt(prompt);

        var completionResults = await (await httpClient.PostAsync(endpoint, chatHistory)).Content.ReadAsStringAsync();
        var deserializedResults = JsonSerializer.Deserialize<OpenAIChatResponse>(completionResults);
        var result = new FunctionResult(name, pluginName, deserializedResults.Choices[0].Message.Content);
        result.Metadata.Add(AIFunctionResultExtensions.ModelResultsMetadataKey, deserializedResults);

        return result;
    }

    public async override Task<FunctionResult> GetModelStreamingResultAsync(string pluginName, string name, string prompt, Dictionary<object, BinaryFile>? files = default)
    {
        throw new NotImplementedException();
    }

    public override List<Type> OutputTypes()
    {
        return new List<Type>
        {
            typeof(string)
        };
    }

    public override List<string> Capabilities()
    {
        return new List<string>
        {
            "chat"
        };
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

    private StringContent ChatHistoryFromPrompt(string prompt)
    {
        ModelRequest modelRequest = modelRequestXmlConverter.ParseXml(prompt);

        List<object> messages = new();

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

            List<object> messageContent = new();
            if (modelMessage.Content is MessageParts contentArray)
            {
                foreach (var content in contentArray)
                {
                    if(content is string textContent)
                    {
                        messageContent.Append(new
                        {
                            type = "text",
                            text = textContent
                        });
                    }
                    else if (content is ImageContent imageContent)
                    {
                        messageContent.Add(new
                        {
                            type = "image_url",
                            image_url = new
                            {
                                url = imageContent.GetSrc()
                            }
                        });
                    }
                }
            }

            messages.Add(new
            {
                role = authorRole.ToString().ToLower(),
                content = messageContent
            });
        }

        // Construct the request body.
        var request = JsonSerializer.Serialize(new
        {
            model = this.ModelId,
            messages = messages,
            max_tokens = 1000,
        });

        return new StringContent(request,
            Encoding.UTF8,
            MediaTypeNames.Application.Json);
    }
}