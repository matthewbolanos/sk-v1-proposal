// Copyright (c) Microsoft. All rights reserved.
using Microsoft.SemanticKernel.Orchestration;
using HandlebarsDotNet;
using Microsoft.SemanticKernel.TemplateEngine;
using System.Text.Json;
using HandlebarsDotNet.Compiler;
using HandlebarsDotNet.IO;

namespace Microsoft.SemanticKernel.Handlebars;

public class HandlebarsPromptTemplateEngine : IPromptTemplateEngine
{

    public HandlebarsPromptTemplateEngine()
    {
    }

    public string Render(IKernel kernel, string template, Dictionary<string,object?> variables, CancellationToken cancellationToken = default)
    {
        IHandlebars handlebarsInstance = HandlebarsDotNet.Handlebars.Create(
            new HandlebarsConfiguration
            {
                NoEscape = true
            });

        // Add helpers for each function
        foreach (FunctionView function in ((Kernel)kernel).GetFunctionViews())
        {
            RegisterFunctionAsHelper(kernel, handlebarsInstance, function, variables, cancellationToken);
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

        handlebarsInstance.RegisterHelper("set", (writer, options, context, arguments) =>
        {
            var stringWriter = ReusableStringWriter.Get();
            using var encodedTextWriter = new EncodedTextWriter(
                stringWriter,
                handlebarsInstance.Configuration.TextEncoder,
                FormatterProvider.Current
            );

            // Render the block content into the StringWriter
            options.Template(encodedTextWriter, context);

            // The block content is now captured in the StringWriter and can be retrieved with stringWriter.ToString()

            // Do something with the block content, e.g., log, save to database, etc.
            string capturedContent = encodedTextWriter.ToString();

            // Get the parameters from the template arguments
            var parameters = arguments[0] as IDictionary<string, object>;

            if (variables.ContainsKey((string)parameters!["name"]))
            {
                variables[(string)parameters!["name"]] = capturedContent;
            }
            else
            {
                variables.Add((string)parameters!["name"], capturedContent);
            }
        });

        handlebarsInstance.RegisterHelper("get", (writer, context, arguments) => 
        {
            string parameter = arguments[0].ToString()!;

            if (variables.ContainsKey(parameter))
            {
                writer.Write(variables[parameter]);
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

    private void RegisterFunctionAsHelper(IKernel kernel, IHandlebars handlebarsInstance, FunctionView functionView, Dictionary<string,object?> variables, CancellationToken cancellationToken = default)
    {
        string fullyResolvedFunctionName = functionView.PluginName + "_" + functionView.Name;

        handlebarsInstance.RegisterHelper(fullyResolvedFunctionName, (writer, context, arguments) =>
        {
            // Get the parameters from the template arguments
            if (arguments.Any())
            {
                if (arguments[0].GetType() == typeof(HashParameterDictionary))
                {
                    // Process hash arguments
                    var handlebarArgs = arguments[0] as IDictionary<string, object>;

                    // Prepare the input parameters for the function
                    foreach (var param in functionView.Parameters)
                    {
                        var fullyQualifiedParamName = functionView.Name + "_" + param.Name;
                        if (param.Type == typeof(IKernel))
                        {
                            variables.Add(param.Name, kernel);
                        }
                        // Check if parameters has key
                        else if (handlebarArgs?.ContainsKey(param.Name) == true || handlebarArgs?.ContainsKey(fullyQualifiedParamName) == true)
                        {
                            var value = handlebarArgs.TryGetValue(param.Name, out var val) ? val : handlebarArgs[fullyQualifiedParamName];
                            if (variables.ContainsKey(param.Name))
                            {
                                variables[param.Name] = value;
                            }
                            else
                            {
                                variables.Add(param.Name, value);
                            }
                        }
                        else if (param.IsRequired == true)
                        {
                            throw new Exception($"Parameter {param.Name} is required for function {functionView.Name}.");
                        }
                    }
                }
                else
                {
                    // Process positional arguments
                    var requiredParameters = functionView.Parameters.Where(p => p.IsRequired == true).ToList();
                    if (arguments.Length >= requiredParameters.Count && arguments.Length <= functionView.Parameters.Count)
                    {
                        var argIndex = 0;
                        foreach (var arg in arguments)
                        {
                            var param = functionView.Parameters[argIndex];
                            if (IsExpectedParameterType(param.Type, arg.GetType(), arg))
                            {
                                if (variables.ContainsKey(param.Name))
                                {
                                    variables[param.Name] = arguments[argIndex];
                                }
                                else
                                {
                                    variables.Add(param.Name, arguments[argIndex]);
                                }
                                argIndex++;
                            }
                            else
                            {
                                throw new Exception($"Invalid parameter type for function {functionView.Name}. Parameter {param.Name} expects type {param.Type} but received {arguments[argIndex].GetType()}.");
                            }
                        }
                    }
                    else
                    {
                        throw new Exception($"Invalid parameter count for function {functionView.Name}. {arguments.Length} were specified but {functionView.Parameters.Count} are required.");
                    }
                }
            }

            ISKFunction function = kernel.Functions.GetFunction(functionView.PluginName, functionView.Name);
            FunctionResult result = function.InvokeAsync(
                kernel,
                variables: variables,
                cancellationToken: cancellationToken
            ).GetAwaiter().GetResult();

            // Write the result to the template
            writer.Write(result);
        });
    }

    private static bool IsNumericType(Type type)
    {
        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Byte:
            case TypeCode.Decimal:
            case TypeCode.Double:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.SByte:
            case TypeCode.Single:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
                return true;
            default:
                return false;
        }
    }

    private static bool TryParseAnyNumber(string input)
    {
        // Check if input can be parsed as any of these numeric types  
        return int.TryParse(input, out _)
            || double.TryParse(input, out _)
            || float.TryParse(input, out _)
            || long.TryParse(input, out _)
            || decimal.TryParse(input, out _)
            || short.TryParse(input, out _)
            || byte.TryParse(input, out _)
            || sbyte.TryParse(input, out _)
            || ushort.TryParse(input, out _)
            || uint.TryParse(input, out _)
            || ulong.TryParse(input, out _);
    }

    /*
     * Type check will pass if:
     * Types are an exact match.
     * Handlebar argument is any kind of numeric type if function parameter requires a numeric type.
     * Handlebar argument type is an object (this covers complex types).
     * Function parameter is a generic type.
     */
    private static bool IsExpectedParameterType (Type functionViewType, Type handlebarArgumentType, object handlebarArgValue)
    {
        var isValidNumericType = IsNumericType(functionViewType) && IsNumericType(handlebarArgumentType);
        if (IsNumericType(functionViewType) && !IsNumericType(handlebarArgumentType))
        {
            isValidNumericType = TryParseAnyNumber(handlebarArgValue.ToString()!);
        }
            
        return functionViewType == handlebarArgumentType || isValidNumericType || handlebarArgumentType == typeof(object) || functionViewType.IsGenericType;
    }

    [Obsolete("Use Render() instead.")]
    public Task<string> RenderAsync(string templateText, SKContext context, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}