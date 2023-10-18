// Copyright (c) Microsoft. All rights reserved.
using Microsoft.SemanticKernel.Orchestration;
using HandlebarsDotNet;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Services;
using Microsoft.SemanticKernel.AI;

namespace Microsoft.SemanticKernel;

public class HandlebarsPromptTemplate : ICompiledPromptTemplate
{
    private readonly string template;
    private readonly IHandlebars handlebarsInstance;

    public HandlebarsPromptTemplate(string template)
    {
        this.handlebarsInstance = Handlebars.Create(
            new HandlebarsConfiguration
            {
                NoEscape = true
            });
        this.template = template;
        handlebarsInstance.RegisterHelper("message", (writer, options, context, arguments) => 
        {
            var parameters = arguments[0] as IDictionary<string, object>;
            writer.Write($"<{parameters["role"]}~>", false);
            options.Template(writer, context);
            writer.Write($"</{parameters["role"]}~>", false);
        });
    }

    public string Render(Dictionary<string, object>? variables, CancellationToken cancellationToken = default)
    {
        var compiledTemplate = handlebarsInstance.Compile(template);
        return compiledTemplate(variables);
    }

    public void AddFunction(string pluginName, AIFunction function, SKContext skContext, IAIService client, AIRequestSettings? requestSettings, CancellationToken cancellationToken = default)
    {
        handlebarsInstance.RegisterHelper(function.Name, (writer, context, arguments) => 
        {
            // Get the parameters from the template arguments
            var parameters = arguments[0] as IDictionary<string, object>;

            // Prepare the input parameters for the function
            var inputParameters = new Dictionary<string, object>();
            foreach (var param in function.InputParameters)
            {
                inputParameters.Add(param.Name, parameters[param.Name]);
            }

            // Run the function
            var result = function.RunAsync(
                pluginName,
                skContext,
                client,
                requestSettings,
                inputParameters,
                cancellationToken
            ).GetAwaiter().GetResult();

            // Write the result to the template
            writer.Write(result);
        });
    }

    public void AddFunction(string pluginName, ISKFunction function)
    {
        throw new NotImplementedException();
    }

    [Obsolete("SKContext is obsolete. Use RenderAsync(Dictionary<string,object>, CancellationToken) instead.")]
    public Task<string> RenderAsync(SKContext executionContext, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    [Obsolete("Parameters are no longer automatically generated. This will be removed in a future release.")]

    public IReadOnlyList<ParameterView> Parameters => throw new NotImplementedException();
}