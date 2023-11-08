// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.SemanticKernel.Handlebars;

/// <summary>
/// HTTP schema to perform completion request.
/// </summary>
[Serializable]
public sealed class SummarizationTaskRequest
{
    [JsonPropertyName("inputs")]
    public string Inputs { get; set; }
}