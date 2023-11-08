// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Microsoft.SemanticKernel.Handlebars;

/// <summary>
/// HTTP Schema for completion response.
/// </summary>
public sealed class QuestionAnsweringTaskResponse
{

    [JsonPropertyName("score")]
    public double? Score { get; set; }

    [JsonPropertyName("start")]
    public int? Start { get; set; }

    [JsonPropertyName("end")]
    public int? End { get; set; }

    [JsonPropertyName("answer")]
    public string? Answer { get; set; }
}

