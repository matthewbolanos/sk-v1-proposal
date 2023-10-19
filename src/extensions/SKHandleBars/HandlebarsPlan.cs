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
using Microsoft.SemanticKernel.Planning;

namespace Microsoft.SemanticKernel.Handlebars;

public sealed class HandlebarsPlan : IPlan
{
    private readonly IKernel Kernel;
    private HandlebarsPromptTemplate PromptTemplate;
    private string Template;

    public HandlebarsPlan(IKernel kernel, string template)
    {
        this.Kernel = kernel;
        this.Template = template;
        PromptTemplate = new HandlebarsPromptTemplate(template);
    }

    public override string ToString()
    {
        return Template;
    }

    public string Name => throw new NotImplementedException();

    public string PluginName => throw new NotImplementedException();

    public string Description => throw new NotImplementedException();

    public AIRequestSettings? RequestSettings => throw new NotImplementedException();

    public string SkillName => throw new NotImplementedException();

    public bool IsSemantic => throw new NotImplementedException();

    public FunctionView Describe()
    {
        throw new NotImplementedException();
    }

    public async Task<FunctionResult> InvokeAsync(
        IKernel kernel,
        SKContext executionContext,
        Dictionary<string, object> variables,
        CancellationToken cancellationToken = default)
    {
        string results = PromptTemplate.Render(kernel, executionContext, variables, cancellationToken);

        return new FunctionResult("Plan", "Planner", executionContext, results);
    }

    public Task<FunctionResult> InvokeAsync(SKContext context, AIRequestSettings? requestSettings = null, CancellationToken cancellationToken = default)
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
}