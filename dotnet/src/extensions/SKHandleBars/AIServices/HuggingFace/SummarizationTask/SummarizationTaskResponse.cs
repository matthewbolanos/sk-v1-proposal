// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Microsoft.SemanticKernel.Handlebars;

/// <summary>
/// HTTP Schema for completion response.
/// </summary>
public sealed class SummarizationTaskResponse
{

    [JsonPropertyName("summary_text")]
    public string? SummaryText { get; set; }
}

