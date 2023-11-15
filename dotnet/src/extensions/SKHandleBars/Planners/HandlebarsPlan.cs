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
using System.Net;

namespace Microsoft.SemanticKernel.Handlebars;

public sealed class HandlebarsPlan : IPlan
{
    private readonly IKernel kernel;
    private readonly List<string> templates;

    public HandlebarsPlan(IKernel kernel, List<string> templates)
    {
        this.kernel = kernel;
        this.templates = templates;
    }

    public override string ToString()
    {
        return String.Join("\n", templates);
    }

    public string Name => throw new NotImplementedException();

    public string PluginName => throw new NotImplementedException();

    public string Description => throw new NotImplementedException();

    public AIRequestSettings? RequestSettings => throw new NotImplementedException();

    public string SkillName => throw new NotImplementedException();

    public bool IsSemantic => throw new NotImplementedException();


    public async Task<FunctionResult> InvokeAsync(
        IKernel kernel,
        Dictionary<string, object?> variables,
        CancellationToken cancellationToken = default)
    {
        string decodedResults = "";
        foreach (var template in templates)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(template);
            try {
                Match match = Regex.Match(template, @"```\s*(handlebars)?\s+(.*)", RegexOptions.Singleline);

                string handlebarsTemplate = "";
                if (match.Success)
                {
                    handlebarsTemplate = match.Groups[2].Value;
                }
                else {
                    handlebarsTemplate = template;
                }

                // Remove ``` at the end of the template
                handlebarsTemplate = Regex.Replace(handlebarsTemplate, @"```\s*$", "", RegexOptions.Singleline);

                string results = await kernel.PromptTemplateEngine.RenderAsync(kernel, handlebarsTemplate, variables, cancellationToken);
                decodedResults = WebUtility.HtmlDecode(results);
                // if (decodedResults != "")
                // {
                //     return new FunctionResult("Plan", "Planner", decodedResults);
                // }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(decodedResults.Trim());
            } catch (Exception e) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message);
            }
            Console.ResetColor();
        }

        return new FunctionResult("Plan", "Planner", decodedResults.Trim());
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

    public Task<Orchestration.FunctionResult> InvokeAsync(SKContext context, AIRequestSettings? requestSettings = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}