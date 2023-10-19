// Copyright (c) Microsoft. All rights reserved.
using Microsoft.SemanticKernel.Orchestration;
using HandlebarsDotNet;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Services;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.TemplateEngine;
using System.Text.Json;

namespace Microsoft.SemanticKernel.Handlebars;

public class HandlebarsPromptTemplate : IPromptTemplate
{
    private readonly string template;
    private readonly IHandlebars handlebarsInstance;

    public HandlebarsPromptTemplate(string template)
    {
        this.handlebarsInstance = HandlebarsDotNet.Handlebars.Create(
            new HandlebarsConfiguration
            {
                NoEscape = true
            });
        this.template = template;
    }

    public string Render(IKernel kernel, SKContext executionContext, Dictionary<string,object> variables, CancellationToken cancellationToken = default)
    {

        // Add helpers for each function
        foreach (FunctionView function in executionContext.Functions.GetFunctionViews())
        {
            RegisterFunctionAsHelper(kernel, executionContext, function, variables, cancellationToken);
        }

        // Add system helpers
        handlebarsInstance.RegisterHelper("message", (writer, options, context, arguments) =>
        {
            var parameters = arguments[0] as IDictionary<string, object>;

            // Verify that the message has a role
            if (!parameters!.ContainsKey("role"))
            {
                throw new Exception("Message must have a role.");
            }

            writer.Write($"<{parameters["role"]}~>", false);
            options.Template(writer, context);
            writer.Write($"</{parameters["role"]}~>", false);
        });

        handlebarsInstance.RegisterHelper("set", (writer, context, arguments) => 
        {
            // Get the parameters from the template arguments
            var parameters = arguments[0] as IDictionary<string, object>;

            if (variables.ContainsKey((string)parameters!["name"]))
            {
                variables[(string)parameters!["name"]] = parameters["value"];
            }
            else
            {
                variables.Add((string)parameters!["name"], parameters["value"]);
            }
        });

        handlebarsInstance.RegisterHelper("json", (writer, context, arguments) => 
        {
            object objectToSerialize = arguments[0];
            string json = JsonSerializer.Serialize(objectToSerialize);

            writer.Write(json);
        });

        handlebarsInstance.RegisterHelper("eq", (writer, context, arguments) => 
        {
            object left = arguments[0];
            object right = arguments[1];

            if (left.Equals(right))
            {
                writer.Write("True");
            }
        });

        handlebarsInstance.RegisterHelper("raw", (writer, options, context, arguments) => {
            options.Template(writer, null);
        });

        var compiledTemplate = handlebarsInstance.Compile(template);
        return compiledTemplate(variables);
    }

    private void RegisterFunctionAsHelper(IKernel kernel, SKContext executionContext, FunctionView functionView, Dictionary<string,object> variables, CancellationToken cancellationToken = default)
    {
        string fullyResolvedFunctionName = functionView.PluginName + "_" + functionView.Name;

        handlebarsInstance.RegisterHelper(fullyResolvedFunctionName, (writer, context, arguments) =>
        {
            // Get the parameters from the template arguments
            if (arguments.Any())
            {
                var parameters = arguments[0] as IDictionary<string, object>;

                // Prepare the input parameters for the function
                foreach (var param in functionView.Parameters)
                {
                    // Check if parameters has key
                    if (parameters?.ContainsKey(param.Name) == true)
                    {
                        if (variables.ContainsKey(param.Name))
                        {
                            variables[param.Name] = parameters[param.Name];
                        }
                        else
                        {
                            variables.Add(param.Name, parameters[param.Name]);
                        }
                    }
                    else if (param.IsRequired == true)
                    {
                        throw new Exception($"Parameter {param.Name} is required for function {functionView.Name}.");
                    }
                }
            }

            ISKFunction function = executionContext.Functions.GetFunction(functionView.PluginName, functionView.Name);
            
            FunctionResult result;
            if (function is HandlebarsAIFunction handlebarsAIFunction)
            {

                result = handlebarsAIFunction.InvokeAsync(
                    kernel,
                    executionContext,
                    variables: variables,
                    cancellationToken: cancellationToken
                ).GetAwaiter().GetResult();
            }
            else
            {
                result = function.InvokeAsync(
                    kernel,
                    executionContext,
                    variables: variables,
                    cancellationToken: cancellationToken
                ).GetAwaiter().GetResult();
            }


            // Write the result to the template
            writer.Write(result);
        });
    }

    [Obsolete("Use Render() instead.")]

    public Task<string> RenderAsync(SKContext executionContext, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    [Obsolete("Parameters are no longer automatically generated. This will be removed in a future release.")]

    public IReadOnlyList<ParameterView> Parameters => throw new NotImplementedException();
}