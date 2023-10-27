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
using Microsoft.SemanticKernel.TemplateEngine;

namespace Microsoft.SemanticKernel.Handlebars;

public static class HandlebarsIPromptTemplateEngineExtensions
{
    public static async Task<string> RenderAsync(
        this IPromptTemplateEngine promptTemplateEngine,
        IKernel kernel,
        string template,
        Dictionary<string,object?> variables,
        CancellationToken cancellationToken = default)
    {
        if (promptTemplateEngine is HandlebarsPromptTemplateEngine)
        {
            return await Task.Run(() => ((HandlebarsPromptTemplateEngine)promptTemplateEngine).Render(kernel, template, variables, cancellationToken));
        }
        else
        {
            throw new Exception("Prompt template engine is not a HandlebarsPromptTemplate.");
        }        
    }
}