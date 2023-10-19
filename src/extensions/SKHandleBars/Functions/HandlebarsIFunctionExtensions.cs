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

namespace Microsoft.SemanticKernel.Handlebars;

public static class HandlebarsIFunctionExtensions
{
    public static async Task<FunctionResult> InvokeAsync(
        this ISKFunction function,
        IKernel kernel,
        SKContext executionContext,
        Dictionary<string, object> variables,
        CancellationToken cancellationToken = default)
    {
        // Populate the execution context with the variables
        foreach (var variable in variables)
        {
            if (executionContext.Variables.ContainsKey(variable.Key))
            {
                executionContext.Variables[variable.Key] = variable.Value.ToString() ?? string.Empty;
            }
            else
            {
                executionContext.Variables.Add(variable.Key, variable.Value.ToString() ?? string.Empty);
            }
        }

        // Invoke the function
        FunctionResult functionResult = await function.InvokeAsync(executionContext, cancellationToken: cancellationToken);

        // Update the variables with the execution context
        // TODO: deserialize the variables from the execution context
        foreach(var variable in executionContext.Variables)
        {
            if (variables.ContainsKey(variable.Key))
            {
                variables[variable.Key] = variable.Value;
            }
            else
            {
                variables.Add(variable.Key, variable.Value);
            }
        }

        return functionResult;
    }
}