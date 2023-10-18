// Copyright (c) Microsoft. All rights reserved.


#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace - Using the namespace of IKernel
using System.Text.RegularExpressions;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Planners;

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
        params ISKFunction[] functions)
    {
        // loop over functions and register them
        foreach (var function in functions)
        {
            // TODO: add a way to include the plugin name
            kernel.RegisterCustomFunction(function);
        }
    }

    public async static Task<string> RunAsync(
        this IKernel kernel,
        Dictionary<string, object>? variables,
        params string[] functions)
    {
        List<Command> commands = new();

        // Loop over functions and create a list of Commands
        foreach (var function in functions)
        {
            var match = Regex.Match(function, @"(?:(?<variable>\w+)\s*=\s*)?(?<function>\w+)\s*\((?<arguments>.+)\)");

            if (match.Success)
            {
                commands.Add(new(){
                    AssignmentVariableName = match.Groups["variable"]?.Value,
                    FunctionName = match.Groups["function"].Value,
                    Arguments = match.Groups["arguments"].Value.Split(',').Select(arg => new Argument()
                    {
                        Name = arg.Split('=')[0].Trim(),
                        Value = arg.Split('=')[1].Trim()
                    }).ToList()
                });
            }
        }

        // Create prompt template
        string template = CreateNestedPrompt(commands);
        var promptTemplate = new HandlebarsPromptTemplate(template);

        // Loop over all functions in the kernel and register them
        foreach(FunctionView functionView in kernel.Functions.GetFunctionViews())
        {
            RegisterHelpers(kernel, functionView.Name, promptTemplate);
        }

        // Run the prompt template
        return promptTemplate.Render(variables);
    }

    private static void RegisterHelpers(IKernel kernel, string functionName, HandlebarsPromptTemplate promptTemplate)
    {
        ISKFunction f = kernel.Functions.GetFunction(functionName);

            if (f is AIFunction)
            {
                promptTemplate.AddFunction(
                    pluginName: "Chat", // TODO: Need to get this from kernel
                    function: (AIFunction)f,
                    skContext: kernel.CreateNewContext(), // TODO: should remove
                    client: kernel.GetService<IChatCompletion>("gpt-35-turbo"),
                    requestSettings: new AIRequestSettings()
                    {
                        ModelId = "gpt-35-turbo",
                    },
                    cancellationToken: default);
            }
            else
            {
                promptTemplate.AddFunction(
                    pluginName: "Search",  // TODO: Need to get this from kernel
                    function: f,
                    skContext: kernel.CreateNewContext() // TODO: should remove
                );
            }
    }

    private static string CreateNestedPrompt(IEnumerable<Command> commands)
    {
        Command command = commands.First();
        string arguments = string.Join(" ", command.Arguments.Select(arg => arg.Name + "=" + arg.Value));
        string function = command.FunctionName + " " + arguments;
        
        if (commands.Count() == 1)
        {
            return "{{" + function + "}}";
        }

        var template = "{{Set (" + function + ")";
        if (command.AssignmentVariableName != null)
        {
            template = "{{Set name=\"" + command.AssignmentVariableName + "\" value=(" + function + ")}}";
        }
        template += CreateNestedPrompt(commands.Skip(1));

        return template;
    }

    internal class Command
    {
        public string? AssignmentVariableName { get; set; }
        public string FunctionName { get; set; }
        public List<Argument> Arguments { get; set; }
    }

    internal class Argument
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
