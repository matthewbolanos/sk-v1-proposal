// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.SemanticKernel.Handlebars;


[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class SKSampleAttribute : Attribute
{
    public string? Inputs { get; }
    public object? Output;
    public string? Error;

    public SKSampleAttribute(string? inputs, object? output = null, string? error = null)
    {
        Inputs = inputs;
        Output = output;
        Error = error;
    }
}