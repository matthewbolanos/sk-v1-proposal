// Copyright (c) Microsoft. All rights reserved.


#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace - Using the namespace of IKernel
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Planners;

namespace Microsoft.SemanticKernel.Handlebars;

public class Plugin
{
    public Plugin(
        string name,
        Collection<ISKFunction> functions,
        string? description = null,
        Uri? logo = null,
        string? contactEmail = null,
        Uri? legalInfoUrl = null)
    {
        Name = name;
        Description = description;
        this.logo = logo;
        ContactEmail = contactEmail;
        LegalInfoUrl = legalInfoUrl;
        Functions = functions;
    }

    public string Name { get; }

    public string? Description  { get; }

    public Uri? logo { get; }

    public string? ContactEmail { get; }

    public Uri? LegalInfoUrl { get; }

    public Collection<ISKFunction> Functions { get; }
}