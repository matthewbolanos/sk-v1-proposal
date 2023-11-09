// Copyright (c) Microsoft. All rights reserved.


#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace - Using the namespace of IKernel
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Planners;

namespace Microsoft.SemanticKernel.Handlebars;

public interface IPlugin
{
    public string Name { get; }

    public string? Description  { get; }

    public IEnumerable<ISKFunction> Functions { get; }
}