// Copyright (c) Microsoft. All rights reserved.
using Microsoft.SemanticKernel.Orchestration;
using HandlebarsDotNet;
using Microsoft.SemanticKernel.TemplateEngine;
using System.Text.Json;
using HandlebarsDotNet.Compiler;
using HandlebarsDotNet.IO;
using System.ComponentModel.DataAnnotations;
using HandlebarsDotNet.Features;
using HandlebarsDotNet.Helpers;
using HandlebarsDotNet.Helpers.Options;

namespace Microsoft.SemanticKernel.Handlebars;

public class HandlebarsPromptTemplateEngine : IPromptTemplateEngine
{
    public HandlebarsPromptTemplateEngine()
    {
    }

    public string Render(IKernel kernel, string template, Dictionary<string,object?> variables, CancellationToken cancellationToken = default)
    {
        
        IHandlebars handlebarsInstance = HandlebarsDotNet.Handlebars.Create();

        // Add helpers for each function
        foreach (FunctionView function in ((Kernel)kernel).GetFunctionViews())
        {
            RegisterFunctionAsHelper(kernel, handlebarsInstance, function, variables, cancellationToken);
        }

        // Add system helpers
        RegisterSystemHelpers(handlebarsInstance, variables);


        var compiledTemplate = handlebarsInstance.Compile(template);
        return compiledTemplate(variables);
    }

    private void RegisterFunctionAsHelper(IKernel kernel, IHandlebars handlebarsInstance, FunctionView functionView, Dictionary<string,object?> variables, CancellationToken cancellationToken = default)
    {
        string fullyResolvedFunctionName = functionView.PluginName + "_" + functionView.Name;

        handlebarsInstance.RegisterHelper(fullyResolvedFunctionName, (in HelperOptions options, in Context context, in Arguments arguments) =>
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

            return result.GetValue<object?>();
        });
    }

    private void RegisterSystemHelpers(IHandlebars handlebarsInstance, Dictionary<string,object?> variables)
    {
        handlebarsInstance.RegisterHelper("array", (in HelperOptions options, in Context context, in Arguments arguments) => 
        {
            // convert all the arguments to an array
            var array = arguments.Select(a => a).ToList();

            return array;
        });
        handlebarsInstance.RegisterHelper("range", (in HelperOptions options, in Context context, in Arguments arguments) => 
        {
            var start = int.Parse(arguments[0].ToString());
            var end = int.Parse(arguments[1].ToString());

            var count = end-start;

            // create array from start to end
            var array = Enumerable.Range(start, count).Select(i => (object)i).ToList();

            return array;
        });

        handlebarsInstance.RegisterHelper("concat", (in HelperOptions options, in Context context, in Arguments arguments) => 
        {
            List<string?> strings = arguments.Select((var) => {
                if (var == null)
                {
                    return null;
                }
                return var!.ToString();
            }).ToList();
            return String.Concat(strings);
        });

        handlebarsInstance.RegisterHelper("equal", (in HelperOptions options, in Context context, in Arguments arguments) =>
        {
            object? left = arguments[0];
            object? right = arguments[1];

            return left == right || (left!=null && left.Equals(right));
        });

        handlebarsInstance.RegisterHelper("lessThan", (in HelperOptions options, in Context context, in Arguments arguments) =>
        {
            double left = CasteToNumber(arguments[0]);
            double right = CasteToNumber(arguments[1]);

            return left < right;

        });

        handlebarsInstance.RegisterHelper("greaterThan", (in HelperOptions options, in Context context, in Arguments arguments) =>
        {
            double left = CasteToNumber(arguments[0]);
            double right = CasteToNumber(arguments[1]);

            return left > right;
        });

        handlebarsInstance.RegisterHelper("lessThanOrEqual", (in HelperOptions options, in Context context, in Arguments arguments) =>
        {
            double left = CasteToNumber(arguments[0]);
            double right = CasteToNumber(arguments[1]);

            return left <= right;
        });

        handlebarsInstance.RegisterHelper("greaterThanOrEqual", (in HelperOptions options, in Context context, in Arguments arguments) =>
        {
            double left = CasteToNumber(arguments[0]);
            double right = CasteToNumber(arguments[1]);

            return left >= right;
        });

        handlebarsInstance.RegisterHelper("json", (in HelperOptions options, in Context context, in Arguments arguments) => 
        {
            object objectToSerialize = arguments[0];
            string json = JsonSerializer.Serialize(objectToSerialize);

            return json;
        });

        handlebarsInstance.RegisterHelper("message", (writer, options, context, arguments) =>
        {
            var parameters = arguments[0] as IDictionary<string, object>;

            // Verify that the message has a role
            if (!parameters!.ContainsKey("role"))
            {
                throw new Exception("Message must have a role.");
            }

            writer.Write($"<message role=\"{parameters["role"]}\">", false);
            options.Template(writer, context);
            writer.Write($"</message>", false);
        });

        handlebarsInstance.RegisterHelper("functions", (writer, options, context, arguments) =>
        {
            writer.Write($"<functions>", false);
            options.Template(writer, context);
            writer.Write($"</functions>", false);
        });

        handlebarsInstance.RegisterHelper("function", (writer, context, arguments) =>
        {
            var parameters = arguments[0] as IDictionary<string, object>;

            if (!parameters!.ContainsKey("pluginName"))
            {
                throw new Exception("Function must have a pluginName.");
            }
            if (!parameters!.ContainsKey("name"))
            {
                throw new Exception("Function must have a name.");
            }

            writer.Write($"<function pluginName=\"{parameters["pluginName"]}\" name=\"{parameters["name"]}\"/>", false);
        });

        handlebarsInstance.RegisterHelper("raw", (writer, options, context, arguments) => {
            options.Template(writer, null);
        });

        handlebarsInstance.RegisterHelper("doubleOpen", (writer, context, arguments) => {
            writer.Write("{{");
        });

        handlebarsInstance.RegisterHelper("doubleClose", (writer, context, arguments) => {
            writer.Write("}}");
        });

        handlebarsInstance.RegisterHelper("toCamelCase", (writer, context, arguments) => {
            var str = arguments[0].ToString()!;

            if ( !string.IsNullOrEmpty(str) && char.IsUpper(str[0])) {
                writer.Write(str.Length == 1 ? char.ToLower(str[0]).ToString() : char.ToLower(str[0]) + str[1..]);
            }
        });


        handlebarsInstance.RegisterHelper("set", (writer, context, arguments) => 
        {
            if (arguments[0].GetType() == typeof(HashParameterDictionary))
            {
                // Get the parameters from the template arguments
                var parameters = arguments[0] as IDictionary<string, object>;

                if (variables.ContainsKey((string)parameters!["name"]))
                {
                    variables[(string)parameters!["name"]] = parameters!["value"];
                }
                else
                {
                    variables.Add((string)parameters!["name"], parameters!["value"]);
                }
                // writer.Write((string)parameters!["name"] + " = " + parameters!["value"]);
            }
            else
            {
                var name = arguments[0].ToString();
                var value = arguments[1];

                if (variables.ContainsKey(name))
                {
                    variables[name] = value;
                }
                else
                {
                    variables.Add(name, value);
                }
                // writer.Write(name + " = " + value);
            }
        });

        handlebarsInstance.RegisterHelper("get", (in HelperOptions options, in Context context, in Arguments arguments) => 
        {
            if (arguments[0].GetType() == typeof(HashParameterDictionary))
            {
                var parameters = arguments[0] as IDictionary<string, object>;
                return variables[(string)parameters!["name"]];
            }
            else
            {
                return variables[arguments[0].ToString()];
            }
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

    private static double CasteToNumber(object? number)
    {
        if (number is int numberInt)
        {
            return numberInt;
        }
        else if (number is double numberDouble)
        {
            return numberDouble;
        }
        else if (number is decimal numberDecimal)
        {
            return (double)numberDecimal;
        }
        else
        {
            return double.Parse(number!.ToString()!);
        }
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