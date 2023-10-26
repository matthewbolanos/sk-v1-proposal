// Copyright (c) Microsoft. All rights reserved.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Orchestration;

namespace Microsoft.SemanticKernel.Handlebars;

internal delegate Task<FunctionResult> NativeFunctionDelegate(
    IKernel kernel,
    Dictionary<string, object?> variables,
    string? pluginName,
    CancellationToken cancellationToken);
