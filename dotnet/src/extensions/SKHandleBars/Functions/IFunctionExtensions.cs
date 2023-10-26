// Copyright (c) Microsoft. All rights reserved.

using SKContext =  Microsoft.SemanticKernel.Orchestration.SKContext;

namespace Microsoft.SemanticKernel.Handlebars;

public static class HandlebarsIFunctionExtensions
{
    public static async Task<FunctionResult> InvokeAsync(
        this ISKFunction function,
        IKernel kernel,
        SKContext executionContext,
        Dictionary<string, object> variables,
        CancellationToken cancellationToken = default)
    {
        FunctionResult functionResult;
        if (function is SemanticFunction semanticFunction)
        {
            return await semanticFunction.InvokeAsync(kernel, executionContext, variables: variables, cancellationToken: cancellationToken);
        }
        if (function is NativeFunction nativeFunction)
        {
            return await nativeFunction.InvokeAsync(kernel, executionContext, variables: variables, cancellationToken: cancellationToken);
        }
        
        throw new Exception("Function is not supported.");
    }

    public static FunctionView Describe2(
        this ISKFunction function)
    {
        FunctionView functionView;
        if (function is SemanticFunction semanticFunction)
        {
            return semanticFunction.Describe();
        }
        if (function is NativeFunction nativeFunction)
        {
            return nativeFunction.Describe();
        }
        
        throw new Exception("Function is not supported.");
    }
}