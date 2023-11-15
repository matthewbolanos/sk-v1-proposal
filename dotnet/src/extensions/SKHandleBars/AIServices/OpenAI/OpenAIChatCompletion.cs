

using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Azure.AI.OpenAI;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.AzureSdk;

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

    public async override Task<FunctionResult> GetModelResultAsync(IKernel kernel, string pluginName, string name, string prompt, Dictionary<object, BinaryFile>? files = default)
    {
        Tuple<ChatHistory, OpenAIRequestSettings>  requestObjects = this.ChatHistoryAndRequestSettingsFromPrompt(kernel, prompt);
        ChatHistory chatHistory = requestObjects.Item1;
        OpenAIRequestSettings requestSettings = requestObjects.Item2;

        var completionResults =  await this.GetChatCompletionsAsync(chatHistory, requestSettings).ConfigureAwait(false);
        var modelResults = completionResults.Select(c => c.ModelResult).ToArray();
        var result = new FunctionResult(name, pluginName, modelResults[0].GetOpenAIChatResult().Choice.Message.Content);
        result.Metadata.Add(AIFunctionResultExtensions.ModelResultsMetadataKey, modelResults);

        return result;
    }

    public async override Task<FunctionResult> GetModelStreamingResultAsync(IKernel kernel, string pluginName, string name, string prompt, Dictionary<object, BinaryFile>? files = default)
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

    // private StringContent ChatHistoryFromPrompt(string prompt)
    // {
    //     ModelRequest modelRequest = modelRequestXmlConverter.ParseXml(prompt);

    //     List<object> messages = new();

    //     foreach(ModelMessage modelMessage in modelRequest.Messages!)
    //     {
    //         AuthorRole authorRole = modelMessage.Role.ToLower() switch
    //         {
    //             "assistant" => AuthorRole.Assistant,
    //             "user" => AuthorRole.User,
    //             "system" => AuthorRole.System,
    //             "function" => AuthorRole.System,
    //             _ => throw new NotImplementedException()
    //         };

    //         List<object> messageContent = new();
    //         if (modelMessage.Content is MessageParts contentArray)
    //         {
    //             foreach (var content in contentArray)
    //             {
    //                 if(content is string textContent)
    //                 {
    //                     messageContent.Add(new
    //                     {
    //                         type = "text",
    //                         text = textContent
    //                     });
    //                 }
    //                 else if (content is ImageContent imageContent)
    //                 {
    //                     messageContent.Add(new
    //                     {
    //                         type = "image_url",
    //                         image_url = new
    //                         {
    //                             url = imageContent.GetSrc()
    //                         }
    //                     });
    //                 }
    //             }
    //         }
    //         else
    //         {
    //             messageContent.Add(new
    //             {
    //                 type = "text",
    //                 text = modelMessage.Content
    //             });
    //         }

    //         messages.Add(new
    //         {
    //             role = authorRole.ToString().ToLower(),
    //             content = messageContent
    //         });
    //     }

    //     // Construct the request body.
    //     var request = JsonSerializer.Serialize(new
    //     {
    //         model = this.ModelId,
    //         messages = messages,
    //         max_tokens = 1000,
    //     });

    //     return new StringContent(request,
    //         Encoding.UTF8,
    //         MediaTypeNames.Application.Json);
    // }

    private Tuple<ChatHistory, OpenAIRequestSettings> ChatHistoryAndRequestSettingsFromPrompt(IKernel kernel, string prompt)
    {
        ModelRequest modelRequest = modelRequestXmlConverter.ParseXml(prompt);

        ChatHistory chatHistory = this.CreateNewChat();
        
        if (modelRequest.Contexts != null && modelRequest.Contexts.ContainsKey("context"))
        {
            chatHistory.AddSystemMessage(modelRequest.Contexts["context"].ToString()!);
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

        OpenAIRequestSettings? requestSettings = new OpenAIRequestSettings();

        if (modelRequest.Contexts != null && modelRequest.Contexts.ContainsKey("functions"))
        {
            var functionsFromPrompt = ((FunctionChoices)modelRequest.Contexts["functions"]).GetContext();
            List<OpenAIFunction> functionDefinitions = new();
            if (functionsFromPrompt.Count > 0)
            {
                foreach(FunctionContent function in functionsFromPrompt)
                {
                    // Get function from kernel
                    FunctionView functionView = kernel.Functions.GetFunction(function.PluginName, function.Name).Describe2(function.PluginName);
                    functionDefinitions.Add(functionView.ToOpenAIFunction());
                }
                requestSettings.Functions = functionDefinitions;
                requestSettings.FunctionCall = OpenAIRequestSettings.FunctionCallAuto;
            } 
        }
        
        if (prompt.StartsWith("<request><message role=\"system\">## Instructions\nExplain how to achieve"))
        {
            requestSettings.ResultsPerPrompt = 1;
            requestSettings.Temperature = 0.3;
            requestSettings.TopP = 1;
            requestSettings.MaxTokens = 2000;
            requestSettings.StopSequences = new List<string>() { "```\n", "``` " };
            requestSettings.TokenSelectionBiases = new Dictionary<int, int>() {
                // Promote
                {28, 3}, // "="
                {198, 3}, // "<newline>"
                {320, 2}, // " ("
                {340, 1}, // ")<newline>"
                {429, 2}, // "=\""
                {446, 1}, // "(\""
                {456, 2}, // "get"
                {751, 1}, // "set"
                {883, 1}, // " )"
                {2556, 2}, // "!--"
                {3052, 2}, // "{{"
                {3500, 1}, // "}}"
                {3954, 2}, // " }}"
                {4640, 2}, // "=("
                {5991, 2}, // " {{"
                {8256, 3}, // " }}<newline>"
                {41404, 1}, // " --}}<newline>"
                {53831, 1}, // ")}}"

                // Decrease
                {2, -2}, // "#"
                {8, -5}, // ")"
                {422, -1}, // " if"
                {909, -5}, // "\")"
                {959, -5}, // "var"
                {1442, -1}, // " If"
                {1817, -2}, // "check"
                {4343, -2}, // " Check"
                {6471, -1}, // " loop"
                {14196, -2}, // "``"
                {22070, -1}, // " Loop"
                {30936, -5}, // "\")))<newline>
                {74694, -2}, // "```"

                // Banned
                {7, -100}, // "("
                {63, -100}, // "`"
                {90, -100}, // "{"
                {92, -100}, // "}"
                {314, -100}, // " {"
                {335, -100}, // " }"
                {439, -100}, // " as"
                {457, -100}, // " }<newline>"
                {482, -100}, // " -"
                {489, -100}, // " +"
                {534, -100}, // "}<newline>"
                {611, -100}, // " /"
                {765, -100}, // " |"
                {6104, -100}, // " While
                {6499, -100}, // "<!--
                {8858, -100}, // "example"
                {1034, -100}, // " %"
                {1151, -100}, // "='"
                {1418, -100}, // " while"
                {1447, -100}, // " +="
                {1464, -100}, // " break"
                {1595, -100}, // " `"
                {1819, -100}, // " (("
                {3556, -100}, // "while"
                {4163, -100}, // "(*
                {4288, -100}, // " random"
                {4712, -100}, // " (*"
                {6110, -100}, // " -="
                {7985, -100}, // ".random"
                {8172, -100}, // "-("
                {9137, -100}, // "break;
                {9317, -100}, // ")}"
                {10505, -100}, // " (-
                {10836, -100}, // " Random"
                {11719, -100}, // "random"
                {12148, -100}, // "/("
                {13666, -100}, // "+("
                {18457, -100}, // " (+
                {24841, -100}, // " {{--"
                {27807, -100}, // ".Random
                {39830, -100}, // "until"
                {40098, -100}, // "{{--"
                {47325, -100}, // " (/
            };
        }

        return new Tuple<ChatHistory, OpenAIRequestSettings>(chatHistory, requestSettings);
    }
}