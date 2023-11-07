// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Microsoft.SemanticKernel.Handlebars;

/// <summary>
/// HTTP Schema for completion response.
/// </summary>
public abstract class BinaryFile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string? ContentType { get; set; }

    public byte[]? Bytes { get; set;}
}


