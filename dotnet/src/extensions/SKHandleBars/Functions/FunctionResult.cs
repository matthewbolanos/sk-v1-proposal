// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;

namespace Microsoft.SemanticKernel.Handlebars;

/// <summary>
/// Function result after execution.
/// </summary>
public sealed class FunctionResult
{
    /// <summary>
    /// Name of executed function.
    /// </summary>
    public string FunctionName { get; internal set; }

    /// <summary>
    /// Name of the plugin containing the function.
    /// </summary>
    public string PluginName { get; internal set; }
    internal object? Value { get; private set; } = null;

    /// <summary>
    /// Metadata for storing additional information about function execution result.
    /// </summary>
    public Dictionary<string, object> Metadata { get; internal set; } = new Dictionary<string, object>();

    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionResult"/> class.
    /// </summary>
    /// <param name="functionName">Name of executed function.</param>
    /// <param name="pluginName">Name of the plugin containing the function.</param>
    public FunctionResult(string functionName, string pluginName, object? value)
    {
        this.FunctionName = functionName;
        this.PluginName = pluginName;
        this.Value = value;
    }

    /// <summary>
    /// Returns function result value.
    /// </summary>
    /// <typeparam name="T">Target type for result value casting.</typeparam>
    /// <exception cref="InvalidCastException">Thrown when it's not possible to cast result value to <typeparamref name="T"/>.</exception>
    public T? GetValue<T>()
    {
        if (this.Value is null)
        {
            return default;
        }

        if (this.Value is T typedResult)
        {
            return typedResult;
        }

        throw new InvalidCastException($"Cannot cast {this.Value.GetType()} to {typeof(T)}");
    }

    /// <summary>
    /// Get typed value from metadata.
    /// </summary>
    public bool TryGetMetadataValue<T>(string key, out T value)
    {
        if (this.Metadata.TryGetValue(key, out object? valueObject) &&
            valueObject is T typedValue)
        {
            value = typedValue;
            return true;
        }

        value = default!;
        return false;
    }

    /// <inheritdoc/>
    public override string ToString() => this.Value?.ToString() ?? base.ToString();
}
