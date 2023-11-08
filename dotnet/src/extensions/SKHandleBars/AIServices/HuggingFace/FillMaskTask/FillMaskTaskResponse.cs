// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Microsoft.SemanticKernel.Handlebars;

/// <summary>
/// HTTP Schema for completion response.
/// </summary>
public sealed class FillMaskTaskResponse
{
    [JsonPropertyName("sequence")]
    public string? Sequence { get; set; }

    [JsonPropertyName("score")]
    public double? Score { get; set; }

    [JsonPropertyName("token")]
    public int? Token { get; set; }

    [JsonPropertyName("token_str")]
    public string? TokenStr { get; set; }

}


