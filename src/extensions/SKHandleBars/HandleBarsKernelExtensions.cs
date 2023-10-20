// Copyright (c) Microsoft. All rights reserved.


#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace - Using the namespace of IKernel
using System.Text.RegularExpressions;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Planners;

namespace Microsoft.SemanticKernel.Handlebars;

public static class HandleBarsKernelExtensions
{
    public static void AddPlugin(
        this IKernel kernel,
        Plugin plugin)
    {
        foreach (var function in plugin.Functions)
        {
            kernel.AddFunction(plugin.Name, function);
        }
    }

    public static void AddFunction(
        this IKernel kernel,
        string pluginName,
        ISKFunction function)
    {
        kernel.RegisterCustomFunction(function);
    }

    public static string RunAsync(
        this IKernel kernel,
        string function,
        Dictionary<string, object> variables
        )
    {
        string template;

        var nameParts = function.Split('.');
        if (nameParts.Length == 1)
        {
            template = "{{" + nameParts[0] + "}}";
        }
        else if (nameParts.Length == 2)
        {
            template = "{{" + nameParts[0] + "_" + nameParts[1] + "}}";
        }
        else
        {
            throw new Exception("Invalid function name.");
        }

        // Create prompt template
        var promptTemplate = new HandlebarsPromptTemplate(template);

        // Run the prompt template
        return promptTemplate.Render(kernel, kernel.CreateNewContext(), variables);
    }
}