// Copyright (c) Microsoft. All rights reserved.

using System.Reflection;
using System.Text.RegularExpressions;

namespace Microsoft.SemanticKernel.Handlebars;

public sealed class HandlebarsPlanner
{
    private readonly IKernel Kernel;

    public HandlebarsPlanner(IKernel kernel)
    {
        this.Kernel = kernel;
    }

    public HandlebarsPlan CreatePlan(string goal, IEnumerable<string>? plugins = null, CancellationToken cancellationToken = default)
    {
        string plannerTemplate;

        Assembly assembly = Assembly.GetExecutingAssembly();
        using (Stream stream = assembly.GetManifestResourceStream("HandlebarPlanner.prompt.yaml")!)
        using (StreamReader reader = new(stream))
        {
            plannerTemplate = reader.ReadToEnd();
        }

        this.Kernel.AddFunction("Planner",
            HandlebarsAIFunction.FromYamlContent(
                "Planner",
                plannerTemplate,
                cancellationToken: cancellationToken
            )
        );

        // Get functions
        var functions = this.Kernel.Functions.GetFunctionViews().Where(f => 
        {
            if (plugins == null)
            {
                return f.PluginName != "Planner";
            }
            else
            {
                return plugins.Contains(f.PluginName);
            }
        }).ToList();

        // Generate the plan
        var result = this.Kernel.RunAsync(
            "Planner.HandlebarPlanner",
            variables: new()
            {
                { "functions", functions},
                { "goal", goal }
            }
        );

        Match match = Regex.Match(result, @"```\s*(handlebars)?\s+(.*?)\s+```", RegexOptions.Singleline);

        if (!match.Success)
        {
            throw new Exception("Could not find the plan in the results");
        }
        
        var template = match.Groups[2].Value;

        return new HandlebarsPlan(Kernel, template);
    }
}