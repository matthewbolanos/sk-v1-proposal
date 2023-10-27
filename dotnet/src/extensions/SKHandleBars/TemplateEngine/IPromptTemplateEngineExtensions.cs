// Copyright (c) Microsoft. All rights reserved.

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