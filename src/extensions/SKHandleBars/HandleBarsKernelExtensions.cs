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

    public static string RunFlow(
        this IKernel kernel,
        Dictionary<string, object> variables,
        params string[] functions)
    {
        List<Command> commands = new();

        // Loop over functions and create a list of Commands
        foreach (var function in functions)
        {
            var match = Regex.Match(function, @"(?:(?<variable>\w+)\s*=\s*)?(?<function>[\w_]+)(?:\s*\((?<arguments>.*)\))?");

            if (match.Success)
            {

                string? variableName = match.Groups["variable"]?.Value;
                string functionName = match.Groups["function"].Value;
                string? arguments = match.Groups["arguments"]?.Value;

                if (string.IsNullOrEmpty(variableName))
                {
                    variableName = null;
                }

                if (string.IsNullOrEmpty(arguments))
                {
                    arguments = null;
                }

                commands.Add(new()
                {
                    AssignmentVariableName = variableName,
                    FunctionName = functionName,
                    Arguments = arguments?.Split(',').Select(arg => new Argument()
                    {
                        Name = arg.Split('=')[0].Trim(),
                        Value = arg.Split('=')[1].Trim()
                    }).ToList() ?? new List<Argument>()
                });
            }
        }

        // Create prompt template
        string template = CreatePromptFromCommands(commands);
        var promptTemplate = new HandlebarsPromptTemplate(template);

        // Run the prompt template
        return promptTemplate.Render(kernel, kernel.CreateNewContext(), variables);
    }

    private static string CreatePromptFromCommands(IEnumerable<Command> commands)
    {
        string template = "";
        foreach(Command command in commands)
        {
            string arguments = command.Arguments == null ? "" : string.Join(" ", command.Arguments.Select(arg => arg.Name + "=" + arg.Value));
            string function = command.FunctionName + " " + arguments;

            if (command.AssignmentVariableName != null)
            {
                template += "{{set name=\"" + command.AssignmentVariableName + "\" value=(" + function + ")}}";
            } else
            {
                template += "{{" + function + "}}";
            }

        }

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
