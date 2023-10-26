// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.SemanticKernel.Handlebars;


public sealed class FunctionException<T> where T : Exception
{
    public string ExceptionType => typeof(T).Name;

    public string Message { get; private set; }

    public FunctionException(T exceptionInstance)
    {
        Message = exceptionInstance.Message;
    }
}