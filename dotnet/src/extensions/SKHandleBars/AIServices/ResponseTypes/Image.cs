// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Microsoft.SemanticKernel.Handlebars;

/// <summary>
/// HTTP Schema for completion response.
/// </summary>
public sealed class Image
{
    public string? ContentType { get; set; }

    public byte[]? Bytes { get; set;}


    public override string ToString() {
        return $"Image: {ContentType} ({Bytes?.Length} bytes)";
    }

}


