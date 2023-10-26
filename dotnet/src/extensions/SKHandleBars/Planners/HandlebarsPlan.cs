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
    private readonly IKernel kernel;
    private readonly string promptTemplate;
    private readonly string template;

    public HandlebarsPlan(IKernel kernel, string template)
    {
        this.kernel = kernel;
        this.template = template;
        promptTemplate = template;
    }

    public override string ToString()
    {
        return template;
    }

    public string Name => throw new NotImplementedException();

    public string PluginName => throw new NotImplementedException();

    public string Description => throw new NotImplementedException();

    public AIRequestSettings? RequestSettings => throw new NotImplementedException();

    public string SkillName => throw new NotImplementedException();

    public bool IsSemantic => throw new NotImplementedException();


    public async Task<FunctionResult> InvokeAsync(
        IKernel kernel,
        SKContext executionContext,
        Dictionary<string, object> variables,
        CancellationToken cancellationToken = default)
    {
        string results = kernel.PromptTemplateEngine.Render(kernel, executionContext, template, variables, cancellationToken);

        return new FunctionResult("Plan", "Planner", results);
    }

    public Task<Orchestration.FunctionResult> InvokeAsync(SKContext context, AIRequestSettings? requestSettings = null, CancellationToken cancellationToken = default)
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

    SemanticKernel.FunctionView ISKFunction.Describe()
    {
        throw new NotImplementedException();
    }
}