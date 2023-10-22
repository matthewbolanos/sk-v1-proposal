// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.SemanticKernel.Handlebars;


[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class SKOutputDescriptionAttribute : Attribute
{
    /// <summary>
    /// Initializes the attribute with the name to use.
    /// </summary>
    /// <param name="name">The name.</param>
    public SKOutputDescriptionAttribute(string description) => this.ReturnDescription = description;

    /// <summary>Gets the specified name.</summary>
    public string ReturnDescription { get; }
}
