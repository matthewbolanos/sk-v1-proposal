// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.SemanticKernel.Handlebars;

/// <summary>
/// HTTP schema to perform completion request.
/// </summary>
[Serializable]
public sealed class QuestionAnsweringTaskRequest
{
    [JsonPropertyName("inputs")]
    public QuestionAnsweringTaskInputs Inputs { get; set; }
}

public sealed class QuestionAnsweringTaskInputs
{
    [JsonPropertyName("question")]
    public string Question { get; set; }

    [JsonPropertyName("context")]
    public string Context { get; set; }
}
