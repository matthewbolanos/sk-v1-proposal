// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Plugins.Web.Bing;

public sealed class Todo
{

    [SKFunction, Description("Creates a new todo item. Returns a JSON object with the new todo item.")]
    public string Create(
        [Description("The name of the todo")] string name,
        [Description("The description of the todo")] string description,
        [Description("The due date of the todo")] DateTime dueDate,
        [Description("The priority of the todo (1-5)")] int priority
    )
    {
        return @"{
            ""id"": ""123"",
            ""name"": ""My todo"",
            ""description"": ""My todo description"",
            ""dueDate"": ""2021-01-01"",
            ""priority"": 1
        }";
    }

    [SKFunction, Description("Updates a new todo item. Returns a JSON object with the new todo item.")]
    public string Update(
        [Description("The ID of the todo")] string id,
        [Description("The name of the todo")] string name,
        [Description("The description of the todo")] string description,
        [Description("The due date of the todo")] DateTime dueDate,
        [Description("The priority of the todo (1-5)")] int priority
    )
    {
        return @"{
            ""id"": ""123"",
            ""name"": ""My todo"",
            ""description"": ""My todo description"",
            ""dueDate"": ""2021-01-01"",
            ""priority"": 1
        }";
    }

    [SKFunction, Description("Deletes a new todo item. Returns a JSON object with the deleted todo item.")]
    public string Delete(
        [Description("The ID of the todo")] string id
    )
    {
        return @"{
            ""id"": ""123"",
            ""name"": ""My todo"",
            ""description"": ""My todo description"",
            ""dueDate"": ""2021-01-01"",
            ""priority"": 1
        }";
    }

    [SKFunction, Description("Retrieves a new todo item. Returns a JSON object with the todo item.")]
    public string Get(
        [Description("The ID of the todo")] string id
    )
    {
        return @"{
            ""id"": ""123"",
            ""name"": ""My todo"",
            ""description"": ""My todo description"",
            ""dueDate"": ""2021-01-01"",
            ""priority"": 1
        }";
    }

    [SKFunction, Description("Searches for a todo. Returns a JSON array with the matching todo items.")]
    public string Search(
        [Description("The query to search for")] string query
    )
    {
        return @"[{
            ""id"": ""123"",
            ""name"": ""My todo"",
            ""description"": ""My todo description"",
            ""dueDate"": ""2021-01-01"",
            ""priority"": 1
        }]";
    }
}