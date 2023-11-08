// Copyright (c) Microsoft. All rights reserved.

using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.AzureSdk;
using Microsoft.SemanticKernel.Orchestration;

namespace Microsoft.SemanticKernel.Handlebars;

public sealed class HandlebarsPlanner
{
    private readonly IKernel Kernel;

    private readonly HandlebarsPlannerConfiguration Configuration;

    public HandlebarsPlanner(IKernel kernel, HandlebarsPlannerConfiguration? configuration = default)
    {
        this.Kernel = kernel;
        this.Configuration = configuration ?? new HandlebarsPlannerConfiguration();
    }
    public async Task<HandlebarsPlan> CreatePlanAsync(string goal, CancellationToken cancellationToken = default)
    {
        string plannerTemplate;

        Assembly assembly = Assembly.GetExecutingAssembly();
        using (Stream stream = assembly.GetManifestResourceStream("HandlebarPlanner.prompt.yaml")!)
        using (StreamReader reader = new(stream))
        {
            plannerTemplate = reader.ReadToEnd();
        }

        var plannerFunction = SemanticFunction.GetFunctionFromYamlContent(
            plannerTemplate,
            cancellationToken: cancellationToken
        );

        // Get functions
        var functions = ((Kernel)this.Kernel).GetFunctionViews().Where(f => 
        {
            var fullyQualifiedName = $"{f.PluginName}.{f.Name}";
            bool shouldInclude = false;
            if (Configuration.IncludedPlugins.Count == 0 && Configuration.IncludedFunctions.Count == 0)
            {
                shouldInclude = true;
            }
            
            if (Configuration.IncludedPlugins.Contains(f.PluginName))
            {
                shouldInclude = true;
            }
            if (Configuration.IncludedFunctions.Contains(fullyQualifiedName))
            {
                shouldInclude = true;
            }
            if (Configuration.ExcludedPlugins.Contains(f.PluginName))
            {
                shouldInclude = false;
            }
            if (Configuration.ExcludedFunctions.Contains(fullyQualifiedName))
            {
                shouldInclude = false;
            }

            return shouldInclude;
        }).ToList();

        // Generate the plan
        var result = await this.Kernel.RunAsync(
            plannerFunction,
            variables: new Dictionary<string, object?>()
            {
                { "functions", functions},
                { "goal", goal },
                { "lastPlan", Configuration.LastPlan },
                { "lastError", Configuration.LastError }
            }
        );

        List<string> templates = new();
        result.TryGetMetadataValue(AIFunctionResultExtensions.ModelResultsMetadataKey, out IEnumerable<ModelResult> results);
        foreach(ModelResult modelResult in results)
        {
            string template = modelResult.GetResult<ChatModelResult>().Choice.Message.Content!;
            
            // Loop over all the functions and replace "." with "_"
            foreach (var function in functions)
            {
                // Replace without case sensitivity
                template = Regex.Replace(template, $"{function.PluginName}.{function.Name}", $"{function.PluginName}_{function.Name}", RegexOptions.IgnoreCase);
            }

            template = template.Replace($"compare.equal", $"equal");
            template = template.Replace($"compare.lessThan", $"lessThan");
            template = template.Replace($"compare.greaterThan", $"greaterThan");
            template = template.Replace($"compare.lessThanOrEqual", $"lessThanOrEqual");
            template = template.Replace($"compare.greaterThanOrEqual", $"greaterThanOrEqual");
            template = template.Replace($"compare.greaterThanOrEqual", $"greaterThanOrEqual");

            template = MinifyHandlebarsTemplate(template);

            // template = template.Replace("#set", "set");
            // template = template.Replace("{{Guess:", "{{! Guess:");

            templates.Add(template);
        }

        return new HandlebarsPlan(Kernel, templates);
    }
    static string MinifyHandlebarsTemplate(string template)
    {
        // This regex pattern matches '{{', then any characters including newlines (non-greedy), then '}}'
        string pattern = @"(\{\{[\s\S]*?}})";

        // Replace all occurrences of the pattern in the input template
        return Regex.Replace(template, pattern, m =>
        {
            // For each match, remove the whitespace within the handlebars, except for spaces
            // that separate different items (e.g., 'json' and '(get')
            return Regex.Replace(m.Value, @"\s+", " ").Replace(" {", "{").Replace(" }", "}").Replace(" )", ")");
        });
    }
}

public class HandlebarsPlannerConfiguration
{
    public HandlebarsPlannerConfiguration(
        List<string>? includedPlugins = default,
        List<string>? excludedPlugins = default,
        List<string>? includedFunctions = default,
        List<string>? excludedFunctions = default,
        HandlebarsPlan? lastPlan = default,
        string? lastError = default
    )
    {
        IncludedPlugins = includedPlugins ?? new List<string>();
        ExcludedPlugins = excludedPlugins ?? new List<string>();
        IncludedFunctions = includedFunctions ?? new List<string>();
        ExcludedFunctions = excludedFunctions ?? new List<string>();
        LastPlan = lastPlan;
        LastError = lastError;
    }
    public List<string> IncludedPlugins { get; set; }
    public List<string> ExcludedPlugins { get; set; }
    public List<string> IncludedFunctions { get; set; }
    public List<string> ExcludedFunctions { get; set; }
    public HandlebarsPlan? LastPlan { get; set; }
    public string? LastError { get; set; }
}