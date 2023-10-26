// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Plugins.Web.Bing;

public sealed class Global
{

    [SKFunction, Description("Sets a variable with the given value so it can be used by other functions")]
    public SKContext Set(
        SKContext skContext,
        [Description("The name of the variable to")] string name,
        [Description("The search query")] string value
    )
    {
        skContext.Variables.Set(name, value);
        return skContext;
    }
}