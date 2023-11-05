// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Services;
using YamlDotNet.Serialization;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.Net;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;

namespace Microsoft.SemanticKernel.Handlebars;

public sealed class SemanticFunction : ISKFunction, IDisposable
{
    public string Name { get; }

    public string PluginName { get; }

    public string Description { get; }

    private List<ExecutionSettingsModel> ExecutionSettings { get; }

    public static SemanticFunction GetFunctionFromYaml(
        string filepath,
        ILoggerFactory? loggerFactory = null,
        CancellationToken cancellationToken = default)
    {
        var yamlContent = File.ReadAllText(filepath);
        return GetFunctionFromYamlContent(yamlContent, loggerFactory, cancellationToken);
    }

    public static SemanticFunction GetFunctionFromYamlContent(
        string yamlContent,
        ILoggerFactory? loggerFactory = null,
        CancellationToken cancellationToken = default)
    {
        var deserializer = new DeserializerBuilder()
            .WithTypeConverter(new ExecutionSettingsModelConverter())
            .Build();

        var skFunction = deserializer.Deserialize<SemanticFunctionModel>(yamlContent);

        List<ParameterView> inputParameters = new List<ParameterView>();
        foreach(var inputParameter in skFunction.InputVariables)
        {
            Type parameterViewType;
            switch(inputParameter.Type)
            {
                case "string":
                    parameterViewType = typeof(string);
                    break;
                case "number":
                    parameterViewType = typeof(double);
                    break;
                case "boolean":
                    parameterViewType = typeof(bool);
                    break;
                default:
                    parameterViewType = typeof(object);
                    break;
            }

            inputParameters.Add(new ParameterView(
                inputParameter.Name,
                parameterViewType,
                inputParameter.Description,
                inputParameter.DefaultValue,
                inputParameter.IsRequired
            ));
        }

        var func = new SemanticFunction(
            functionName: skFunction.Name,
            template: skFunction.Template,
            templateFormat: skFunction.TemplateFormat,
            description: skFunction.Description,
            inputParameters: inputParameters,
            // outputParameter: skFunction.OutputVariable,
            executionSettings: skFunction.ExecutionSettings,
            loggerFactory: loggerFactory
        );

        return func;
    }

    public SemanticFunction(
        string functionName,
        string template,
        string templateFormat,
        string description,
        List<ParameterView> inputParameters,
        // SKVariableView outputParameter,
        List<ExecutionSettingsModel> executionSettings,
        ILoggerFactory? loggerFactory = null)
    {
        this._logger = loggerFactory is not null ? loggerFactory.CreateLogger(typeof(SemanticFunction)) : NullLogger.Instance;

        // Add logic to use the right template engine based on the template format
        this.PromptTemplate = template;
        this.PluginName = "";

        this.InputParameters = inputParameters;

        this.Name = functionName;
        this.Description = description;
        this.ExecutionSettings = executionSettings!;

    }

    public FunctionView Describe()
    {   
        return new FunctionView(
            this.Name,
            this.PluginName,
            this.Description,
            this.InputParameters
        );
    }

    public async Task<FunctionResult> InvokeAsync(
        IKernel kernel,
        Dictionary<string, object?> variables,
        CancellationToken cancellationToken = default,
        bool streaming = false
    )
    {
        IAIService? client = null;
        foreach(ExecutionSettingsModel executionSettings in this.ExecutionSettings)
        {
            foreach(IAIService aIService in ((Kernel)kernel).GetAllServices())
            {
                if (aIService is AIService service)
                {
                    if (executionSettings.ModelId == service.ModelId)
                    {
                        client = aIService;
                        break;
                    }

                    // check if regex matches
                    if (executionSettings.ModelIdPattern != null && Regex.IsMatch(service.ModelId, executionSettings.ModelIdPattern))
                    {
                        client = aIService;
                        break;
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            if (client != null)
            {
                break;
            }
        }

        client ??= ((Kernel)kernel).GetDefaultService();

        FunctionResult result;

        // Render the prompt
        string renderedPrompt = await kernel.PromptTemplateEngine.RenderAsync(kernel, PromptTemplate, variables, cancellationToken);

        if(client is IChatCompletion completion)
        {

            // Extract the chat history from the rendered prompt
            string pattern = @"<(user|system|assistant)>(.*?)<\/\1>";
            MatchCollection matches = Regex.Matches(renderedPrompt, pattern, RegexOptions.Singleline);

            // Add the chat history to the chat
            ChatHistory chatMessages = completion.CreateNewChat();
            foreach (Match match in matches.Cast<Match>())
            {
                string role = match.Groups[1].Value;
                string message = WebUtility.HtmlDecode(match.Groups[2].Value);

                switch(role)
                {
                    case "user":
                        chatMessages.AddUserMessage(message);
                        break;
                    case "system":
                        chatMessages.AddSystemMessage(message);
                        break;
                    case "assistant":
                        chatMessages.AddAssistantMessage(message);
                        break;
                }
            }
            
            if (streaming)
            {
                ConfiguredCancelableAsyncEnumerable<IChatStreamingResult> completionResults = completion.GetStreamingChatCompletionsAsync(chatMessages, cancellationToken: cancellationToken).ConfigureAwait(false);
                result = new FunctionResult(this.Name, this.PluginName, ConvertToStrings(completionResults), ConvertToFinalStringAsync(completionResults));
            }
            else
            {
                OpenAIRequestSettings? requestSettings = new OpenAIRequestSettings();
                if (renderedPrompt.StartsWith("<system>## Instructions\nExplain how to achieve"))
                {
                    requestSettings.ResultsPerPrompt = 1;
                    requestSettings.Temperature = 0.5;
                    requestSettings.TopP = 1;
                    requestSettings.MaxTokens = 2000;
                    requestSettings.StopSequences = new List<string>() { "```\n", "``` " };
                    requestSettings.TokenSelectionBiases = new Dictionary<int, int>() {
                        // Promote
                        {28, 3}, // "="
                        {198, 2}, // "<newline>"
                        {320, 2}, // " ("
                        {340, 1}, // ")<newline>"
                        {429, 2}, // "=\""
                        {446, 1}, // "(\""
                        {456, 2}, // "get"
                        {751, 1}, // "set"
                        {883, 1}, // " )"
                        {2556, 1}, // "!--"
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

                IReadOnlyList<IChatResult> completionResults = await completion.GetChatCompletionsAsync(chatMessages, requestSettings, cancellationToken: cancellationToken).ConfigureAwait(false);
                var modelResults = completionResults.Select(c => c.ModelResult).ToArray();

                result = new FunctionResult(this.Name, this.PluginName, modelResults[0].GetOpenAIChatResult().Choice.Message.Content);
                result.Metadata.Add(AIFunctionResultExtensions.ModelResultsMetadataKey, modelResults);
            }
        }
        else
        {
            throw new NotImplementedException();
        }

        return result;
    }

    private async IAsyncEnumerable<string> ConvertToStrings(ConfiguredCancelableAsyncEnumerable<IChatStreamingResult> completionResults)
    {
        await foreach (var result in completionResults)
        {
            await foreach (var message in result.GetStreamingChatMessageAsync())
            {
                yield return message.Content;
            }
        }
    }

    private async Task<string> ConvertToFinalStringAsync(ConfiguredCancelableAsyncEnumerable<IChatStreamingResult> completionResults)
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

    private readonly ILogger _logger;
    private string PromptTemplate { get; }

    private IReadOnlyList<ParameterView> InputParameters { get; }

    public AIRequestSettings? RequestSettings => throw new NotImplementedException();

    public string SkillName => throw new NotImplementedException();

    public bool IsSemantic => throw new NotImplementedException();


    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public ISKFunction SetAIConfiguration(AIRequestSettings? requestSettings)
    {
        throw new NotImplementedException();
    }

    public ISKFunction SetAIService(Func<ITextCompletion> serviceFactory)
    {
        throw new NotImplementedException();
    }

    public ISKFunction SetDefaultFunctionCollection(IReadOnlyFunctionCollection functions)
    {
        throw new NotImplementedException();
    }

    public ISKFunction SetDefaultSkillCollection(IReadOnlyFunctionCollection skills)
    {
        throw new NotImplementedException();
    }

    Task<Orchestration.FunctionResult> ISKFunction.InvokeAsync(Orchestration.SKContext context, AIRequestSettings? requestSettings, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    SemanticKernel.FunctionView ISKFunction.Describe()
    {
        throw new NotImplementedException();
    }
}