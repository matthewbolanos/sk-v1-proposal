// Copyright (c) Microsoft. All rights reserved.


#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace - Using the namespace of IKernel

using Microsoft.SemanticKernel.Orchestration;

namespace Microsoft.SemanticKernel.Handlebars;

public static class HandlebarsIKernelExtensions
{
    public static void AddPlugin(
        this IKernel kernel,
        Plugin plugin)
    {
        foreach (var function in plugin.Functions)
        {
            kernel.AddFunction(plugin.Name, function);
        }
    }

    public static void AddFunction(
        this IKernel kernel,
        string pluginName,
        ISKFunction function)
    {
        kernel.RegisterCustomFunction(function);
    }

    public static async Task<FunctionResult> RunAsync(
        this IKernel kernel,
        ISKFunction function,
        Dictionary<string, object?> variables
        )
    {
        if (kernel is Kernel kernel2)
        {
            return await function.InvokeAsync(kernel, variables);
        }
        else
        {
            throw new Exception("Kernel is not a HandlebarsKernel.");
        }
    }

    public static async Task<FunctionResult> RunAsync(
        this IKernel kernel,
        Dictionary<string, object?> variables
        )
    {
        if (kernel is Kernel kernel2 && kernel2.EntryPoint != null)
        {
            return await kernel2.RunAsync(kernel2.EntryPoint, variables);
        }
        else
        {
            throw new Exception("Entry point not provided.");
        }
    }
}