// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.SemanticKernel.Handlebars;


public sealed class FunctionSample
{
    public Dictionary<string, object?> SampleInputs;

    public object SampleOutput; 

    public FunctionSample(Dictionary<string, object?> sampleInputs, object sampleOutput)
    {
        SampleInputs = sampleInputs;
        SampleOutput = sampleOutput;
    } 
}
