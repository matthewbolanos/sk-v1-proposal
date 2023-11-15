// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.TextCompletion;
using YamlDotNet.Serialization;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.Net;
using Azure.AI.OpenAI;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.AzureSdk;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;
using System.Text.Json;
using Microsoft.SemanticKernel.AI.ChatCompletion;

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
        if (skFunction.InputVariables is not null)
        {foreach(var inputParameter in skFunction.InputVariables)
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

    public FunctionView Describe(string pluginName = "")
    {   
        return new FunctionView(
            this.Name,
            pluginName,
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
        AIService? client = null;
        foreach(ExecutionSettingsModel executionSettings in this.ExecutionSettings)
        {
            foreach(AIService aIService in ((Kernel)kernel).GetAllServices())
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

        client ??= (AIService)((Kernel)kernel).GetDefaultService();

        if (!variables.ContainsKey("functions"))
        {
            variables["functions"] = ((Kernel)kernel).GetFunctionViews();
        }

        FunctionResult result;

        // Render the prompt
        string renderedPrompt = await kernel.PromptTemplateEngine.RenderAsync(kernel, PromptTemplate, variables, cancellationToken);
        renderedPrompt = "<request>" + renderedPrompt + "</request>";
  
        if (streaming)
        {
            result = await client.GetModelStreamingResultAsync(kernel, this.PluginName, this.Name, renderedPrompt);
        }
        else
        {
            result = await client.GetModelResultAsync(kernel, this.PluginName, this.Name, renderedPrompt);
        }

        return result;
    }

    private async Task<FunctionResult> InvokeFunctionCall(IKernel kernel, string name, string arguments, IChatCompletion completion, ChatHistory chatMessages)
    {
        // split name
        string[] nameParts = name.Split("-");

        // get function from kernel
        var function = kernel.Functions.GetFunction(nameParts[0], nameParts[1]);
        // TODO: change back to Dictionary<string, object>
        Dictionary<string, object> variables = JsonSerializer.Deserialize<Dictionary<string, object>>(arguments)!;

        var results = await kernel.RunAsync(
            function,
            variables: variables!
        );

        // temporarily add results as system message to chat history
        chatMessages.AddSystemMessage("Here is some grounding information:\n"+results.GetValue<string>());

        IReadOnlyList<IChatResult> completionResults = await completion.GetChatCompletionsAsync(chatMessages).ConfigureAwait(false);
        var modelResults = completionResults.Select(c => c.ModelResult).ToArray();
        // remove from chat history to put it back to normal
        chatMessages.RemoveAt(chatMessages.Count()-1);

        return new FunctionResult(this.Name, this.PluginName, modelResults[0].GetOpenAIChatResult().Choice.Message.Content);;
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