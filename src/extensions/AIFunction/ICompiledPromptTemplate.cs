// Copyright (c) Microsoft. All rights reserved.
using HandlebarsDotNet;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.TemplateEngine;

namespace Microsoft.SemanticKernel;

public interface ICompiledPromptTemplate : IPromptTemplate
{
    string Render(Dictionary<string,object> variables, CancellationToken cancellationToken = default);
}