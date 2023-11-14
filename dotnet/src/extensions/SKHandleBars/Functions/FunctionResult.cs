// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;

namespace Microsoft.SemanticKernel.Handlebars;

/// <summary>
/// Function result after execution.
/// </summary>
public sealed class FunctionResult
{
    public string FunctionName { get; internal set; }

    public string? PluginName { get; internal set; }

    public Dictionary<string, object> Metadata { get; internal set; } = new Dictionary<string, object>();

    private readonly bool isStreaming = false;

    private readonly object? value = null;

    private readonly IAsyncEnumerable<object?>? streamingValue = null;

    private readonly Task? finalValue = null;

    public FunctionResult(string functionName, string? pluginName, IAsyncEnumerable<object?> streamingValue, Task finalValue)
    {
        this.FunctionName = functionName;
        this.PluginName = pluginName;
        this.streamingValue = streamingValue;
        this.finalValue = finalValue;
        isStreaming = true;
    }
    public FunctionResult(string functionName, string? pluginName, object? value = default)
    {
        this.FunctionName = functionName;
        this.PluginName = pluginName;
        this.value = value;
    }

    public async Task<T>? GetValueAsync<T>()
    {
        if (isStreaming)
        {
            if (this.finalValue is null)
            {
                throw new InvalidOperationException("Cannot get final value from streaming result.");
            }
            if (this.finalValue is Task<T> typedTaskResult)
            {
                return await typedTaskResult;
            }

            throw new InvalidCastException($"Cannot cast {this.finalValue.GetType()} to {typeof(List<T>)}");
        }

        if (this.value is null)
        {
            return default!;
        }

        if (this.value is T typedResult)
        {
            return typedResult;
        }

        throw new InvalidCastException($"Cannot cast {this.value.GetType()} to {typeof(T)}");
    }


    public T? GetValue<T>()
    {
        if (isStreaming)
        {
            throw new InvalidOperationException("Cannot get value from streaming result; use GetValueAsync instead.");
        }

        if (this.value is null)
        {
            return default!;
        }

        if (this.value is T typedResult)
        {
            return typedResult;
        }

        throw new InvalidCastException($"Cannot cast {this.value.GetType()} to {typeof(T)}");
    }

    public IAsyncEnumerable<T>? GetStreamingValue<T>()
    {
        if (isStreaming)
        {
            if (this.streamingValue is null)
            {
                throw new InvalidOperationException("Cannot get streaming value from non-streaming result.");
            }

            if (this.streamingValue is IAsyncEnumerable<T> typedResult)
            {
                return typedResult;
            }

            throw new InvalidCastException($"Cannot cast {this.streamingValue.GetType()} to {typeof(IAsyncEnumerable<T>)}");
        }

        throw new InvalidOperationException("Cannot get streaming value from non-streaming result.");
    }

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

    public override string? ToString() => this.value?.ToString() ?? base.ToString();
}
