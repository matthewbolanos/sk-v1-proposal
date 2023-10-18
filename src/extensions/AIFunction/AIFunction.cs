// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Services;
using YamlDotNet.Serialization;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using System.Text.RegularExpressions;

#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace - Using the main namespace
namespace Microsoft.SemanticKernel;
#pragma warning restore IDE0130

#pragma warning disable format

/// <summary>
/// A Semantic Kernel "Semantic" prompt function.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class AIFunction : ISKFunction, IDisposable
{
    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public string Description { get; }

    public IReadOnlyList<SKVariableView> InputParameters { get; }
    public SKVariableView OutputParameter { get; }

    /// <summary>
    /// Create a semantic function instance, given a semantic function configuration.
    /// </summary>
    /// <param name="pluginName">Name of the plugin to which the function being created belongs.</param>
    /// <param name="functionName">Name of the function to create.</param>
    /// <param name="promptTemplateConfig">Prompt template configuration.</param>
    /// <param name="promptTemplate">Prompt template.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use for logging. If null, no logging will be performed.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>SK function instance.</returns>
    public static AIFunction FromYaml(
        string filepath,
        ILoggerFactory? loggerFactory = null,
        CancellationToken cancellationToken = default)
    {
        var deserializer = new DeserializerBuilder()
            .Build();

        var yamlContent = File.ReadAllText(filepath);
        var skFunction = deserializer.Deserialize<AIFunctionModel>(yamlContent);

        var func = new AIFunction(
            functionName: skFunction.Name,
            template: skFunction.Template,
            templateFormat: skFunction.TemplateFormat,
            description: skFunction.Description,
            inputParameters: skFunction.InputVariables,
            outputParameter: skFunction.OutputVariable,
            loggerFactory: loggerFactory
        );

        return func;
    }

    /// <summary>
    /// Dispose of resources.
    /// </summary>
    public void Dispose()
    {
    }

    public AIFunction(
        string functionName,
        string template,
        string templateFormat,
        string description,
        List<SKVariableView> inputParameters,
        SKVariableView outputParameter,
        ILoggerFactory? loggerFactory = null)
    {
        this._logger = loggerFactory is not null ? loggerFactory.CreateLogger(typeof(AIFunction)) : NullLogger.Instance;

        // Add logic to use the right template engine based on the template format
        this._promptTemplate = new HandlebarsPromptTemplate(template);

        this.InputParameters = inputParameters;
        this.OutputParameter = outputParameter;

        this.Name = functionName;
        this.Description = description;

        this.PluginName = "_GLOBAL_FUNCTIONS_";
    }

    public async Task<FunctionResult> RunAsync(
        string pluginName,
        SKContext context, // TODO: Remove this parameter
        IAIService client,
        AIRequestSettings? requestSettings,
        Dictionary<string,object>? variables,
        CancellationToken cancellationToken = default)
        
    {
        FunctionResult result;

        try
        {
            if(client is IChatCompletion)
            {
                // Generate the prompt using the template
                variables ??= new Dictionary<string, object>();
                string renderedPrompt = this._promptTemplate.Render(variables, cancellationToken);

                // Extract the chat history from the rendered prompt
                string pattern = @"<(user~|system~|assistant~)>(.*?)<\/\1>";
                MatchCollection matches = Regex.Matches(renderedPrompt, pattern, RegexOptions.Singleline);

                // Add the chat history to the chat
                ChatHistory chatMessages = ((IChatCompletion)client).CreateNewChat();
                foreach (Match match in matches)
                {
                    string role = match.Groups[1].Value;
                    string message = match.Groups[2].Value;

                    switch(role)
                    {
                        case "user~":
                            chatMessages.AddUserMessage(message);
                            break;
                        case "system~":
                            chatMessages.AddSystemMessage(message);
                            break;
                        case "assistant~":
                            chatMessages.AddAssistantMessage(message);
                            break;
                    }
                }
                
                // Get the completions
                IReadOnlyList<IChatResult> completionResults = await ((IChatCompletion)client).GetChatCompletionsAsync(chatMessages, requestSettings, cancellationToken).ConfigureAwait(false);
                var modelResults = completionResults.Select(c => c.ModelResult).ToArray();
                result = new FunctionResult(this.Name, pluginName, context, modelResults[0].GetOpenAIChatResult().Choice.Message.Content);
                result.Metadata.Add(AIFunctionResultExtensions.ModelResultsMetadataKey, modelResults);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        catch (Exception ex)
        {
            throw ex;
        }

        return result;
    }

    #region private

    private readonly ILogger _logger;
    private ICompiledPromptTemplate _promptTemplate { get; }


    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => $"{this.Name} ({this.Description})";

    #endregion

    #region Obsolete


    /// <inheritdoc/>
    [Obsolete("Functions no longer have a PluginName property. It is managed by the Kernel. This will be removed in a future release.")]
    public string PluginName { get; }


    /// <inheritdoc/>
    [Obsolete("AI functions will now have multiple request settings. This will be removed in a future release")]

    public AIRequestSettings? RequestSettings { get; private set; }

    /// <inheritdoc/>
    [Obsolete("SKFunctions now always get the AI service from the Kernel. This will be removed in a future release.")]
    public ISKFunction SetAIService(Func<ITextCompletion> serviceFactory)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    [Obsolete("The describe function will removed in a future release")]
    public FunctionView Describe()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    [Obsolete("There are now multiple request settings. This will be removed in a future release.")]
    public ISKFunction SetAIConfiguration(AIRequestSettings? requestSettings)
    {
        this.RequestSettings = requestSettings;
        return this;
    }


    /// <inheritdoc/>
    [Obsolete("Invoke Async will be removed in a future release. Use Kernel.RunAsync instead")]
    public async Task<FunctionResult> InvokeAsync(
        SKContext context,
        AIRequestSettings? requestSettings = null,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    [Obsolete("Methods, properties and classes which include Skill in the name have been renamed. Use ISKFunction.PluginName instead. This will be removed in a future release.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public string SkillName => this.PluginName;

    /// <inheritdoc/>
    [Obsolete("Kernel no longer differentiates between Semantic and Native functions. This will be removed in a future release.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool IsSemantic => true;

    /// <inheritdoc/>
    [Obsolete("This method is a nop and will be removed in a future release.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ISKFunction SetDefaultSkillCollection(IReadOnlyFunctionCollection skills) => this;

    /// <inheritdoc/>
    [Obsolete("This method is a nop and will be removed in a future release.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ISKFunction SetDefaultFunctionCollection(IReadOnlyFunctionCollection functions) => this;

    #endregion
}