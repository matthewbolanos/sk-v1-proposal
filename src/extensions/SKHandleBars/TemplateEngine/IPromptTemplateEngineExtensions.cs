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
    public static string Render(
        this IPromptTemplateEngine promptTemplateEngine,
        IKernel kernel,
        SKContext executionContext,
        string template,
        Dictionary<string,object> variables,
        CancellationToken cancellationToken = default)
    {
        if (promptTemplateEngine is HandlebarsPromptTemplateEngine)
        {
            return ((HandlebarsPromptTemplateEngine)promptTemplateEngine).Render(kernel, executionContext, template, variables, cancellationToken);
        }
        else
        {
            throw new Exception("Prompt template engine is not a HandlebarsPromptTemplate.");
        }        
    }
}