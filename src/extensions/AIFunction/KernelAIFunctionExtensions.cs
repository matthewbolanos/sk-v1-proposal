// Copyright (c) Microsoft. All rights reserved.


#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace - Using the namespace of IKernel
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Orchestration;

namespace Microsoft.SemanticKernel;
#pragma warning restore IDE0130

/// <summary>
/// Class for extensions methods to define semantic functions.
/// </summary>
public static class KernelAIFunctionExtensions
{
    public static void AddFunctions(
        this IKernel kernel,
        string pluginName,
        params AIFunction[] functions)
    {
        // loop over functions and register them
        foreach (var function in functions)
        {
            // TODO: add a way to include the plugin name
            kernel.RegisterCustomFunction(function);
        }
    }

    public async static Task<string> RunFlowAsync(
        this IKernel kernel,
        Dictionary<string, object>? variables,
        params string[] functions)
    {
        // Create prompt template
        string template = createNestedPrompt(functions);
        var promptTemplate = new HandlebarsPromptTemplate(template);

        // Loop over functions and add them to the promptTemplate
        foreach (var function in functions)
        {
            // Split by space
            var functionParts = function.Split(' ');
            var f = kernel.Functions.GetFunction(functionParts[0]);

            if (f is AIFunction)
            {
                promptTemplate.AddFunction(
                    pluginName: "Chat",
                    function: (AIFunction)f,
                    skContext: kernel.CreateNewContext(),
                    client: kernel.GetService<IChatCompletion>("gpt-35-turbo"),
                    requestSettings: new AIRequestSettings()
                    {
                        ModelId = "gpt-35-turbo",
                    },
                    cancellationToken: default);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        // Run the prompt template
        return promptTemplate.Render(variables);
    }

    private static string createNestedPrompt(string[] functions)
    {
        if (functions.Length == 1)
        {
            return "{{" + functions[0] + "}}";
        }

        var template = "{{#with " + functions[0] + "}}";
        template += createNestedPrompt(functions[1..]);
        template += "{{/with}}";
        return template;
    }
}
