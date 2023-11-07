// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.SemanticKernel.Handlebars;

/// <summary>
/// HTTP schema to perform completion request.
/// </summary>
[Serializable]
public sealed class TextToImageTaskRequest
{
    /// <summary>
    /// Prompt to complete.
    /// </summary>
    [JsonPropertyName("inputs")]
    public string Input { get; set; }
}
