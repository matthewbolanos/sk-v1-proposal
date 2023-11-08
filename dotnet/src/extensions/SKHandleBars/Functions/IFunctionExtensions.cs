// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Handlebars;

public static class HandlebarsIFunctionExtensions
{
    public static async Task<FunctionResult> InvokeAsync(
        this ISKFunction function,
        IKernel kernel,
        Dictionary<string, object?> variables,
        bool streaming = false,
        CancellationToken cancellationToken = default
    )
    {
        if (function is SemanticFunction semanticFunction)
        {
            return await semanticFunction.InvokeAsync(kernel, variables: variables, cancellationToken: cancellationToken, streaming: streaming);
        }
        if (function is NativeFunction nativeFunction)
        {
            return await nativeFunction.InvokeAsync(kernel, variables: variables, cancellationToken: cancellationToken,  streaming: streaming);
        }
        if (function is OpenAIThread openAIThread)
        {
            return await openAIThread.InvokeAsync(kernel, variables: variables, cancellationToken: cancellationToken,  streaming: streaming);
        }
        
        throw new Exception("Function is not supported.");
    }

    public static FunctionView Describe2(
        this ISKFunction function, string pluginName = "")
    {
        if (function is SemanticFunction semanticFunction)
        {
            return semanticFunction.Describe(pluginName);
        }
        if (function is NativeFunction nativeFunction)
        {
            return nativeFunction.Describe(pluginName);
        }
        
        throw new Exception("Function is not supported.");
    }
}