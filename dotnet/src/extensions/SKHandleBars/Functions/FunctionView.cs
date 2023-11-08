// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.AzureSdk;

namespace Microsoft.SemanticKernel.Handlebars;

/// <summary>
/// A function view is a read-only representation of a function.
/// </summary>
/// <param name="Name">Name of the function. The name is used by the function collection and in prompt templates e.g. {{pluginName.functionName}}</param>
/// <param name="PluginName">Name of the plugin containing the function. The name is used by the function collection and in prompt templates e.g. {{pluginName.functionName}}</param>
/// <param name="Description">Function description. The description is used in combination with embeddings when searching relevant functions.</param>
/// <param name="Parameters">Optional list of function parameters</param>
public sealed record FunctionView(
    string Name,
    string PluginName,
    string Description = "",
    IReadOnlyList<ParameterView>? Parameters = null)
{

    /// <summary>
    /// List of function parameters
    /// </summary>
    public IReadOnlyList<ParameterView> Parameters { get; init; } = Parameters ?? Array.Empty<ParameterView>();

    public OpenAIFunction ToOpenAIFunction()
    {
        var openAIParams = new List<OpenAIFunctionParameter>();
        foreach (ParameterView param in this.Parameters)
        {
            openAIParams.Add(new OpenAIFunctionParameter
            {
                Name = param.Name,
                Description = (param.Description ?? string.Empty)
                    + (string.IsNullOrEmpty(param.DefaultValue) ? string.Empty : $" (default value: {param.DefaultValue})"),
                Type = param.Type?.Name.ToLower() ?? "string",
                IsRequired = param.IsRequired ?? false
            });
        }

        return new OpenAIFunction
        {
            FunctionName = this.Name,
            PluginName = this.PluginName,
            Description = this.Description,
            Parameters = openAIParams,
        };
    }
}
