// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.Plugins.Core;
using Microsoft.SemanticKernel.AI.ChatCompletion;

// Setup
//////////////////////////////////////////////////////////////////////////////////


string pluginsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "plugins");
using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
  builder
      .SetMinimumLevel(0)
      .AddDebug();
});


// Scenario 1: Run native function
//////////////////////////////////////////////////////////////////////////////////

if (false)
{
  Console.WriteLine("\n\nStarting scenario 1");
  Console.WriteLine("/////////////////////////////////////////////////////\n");

  IKernel kernel = new KernelBuilder()
      .WithLoggerFactory(loggerFactory)
      //.WithFunctions
      .Build();

  // TODO: this should be part of the kernel builder
  kernel.ImportFunctions(new TimePlugin(), "TimePlugin");

  KernelResult result = await kernel.RunAsync(
      kernel.Functions.GetFunction("TimePlugin", "Today")
  );

  // TODO: need a way to simplify this, for example:
  // KernelResult result = await kernel.RunAsync(
  //     "TimePlugin.Today"
  // );

  Console.WriteLine(result.GetValue<string>());
}



// Scenario 2: Run semantic function
//////////////////////////////////////////////////////////////////////////////////

if (true)
{
  Console.WriteLine("\n\nStarting scenario 2");
  Console.WriteLine("/////////////////////////////////////////////////////\n");

  string AzureOpenAIDeploymentName = Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!;
  string AzureOpenAIEndpoint = Env.Var("AzureOpenAI:Endpoint")!;
  string AzureOpenAIApiKey = Env.Var("AzureOpenAI:ApiKey")!;

  IKernel kernel = new KernelBuilder()
      .WithLoggerFactory(loggerFactory)
      .WithAzureChatCompletionService(
          AzureOpenAIDeploymentName,  // The name of your deployment (e.g., "gpt-35-turbo")
          AzureOpenAIEndpoint,        // The endpoint of your Azure OpenAI service
          AzureOpenAIApiKey           // The API key of your Azure OpenAI service
      )
      //.WithSemanticFunctionsFromDirectory
      .Build();

  // TODO: This should be part of the kernel builder
  kernel.ImportSemanticFunctionsFromDirectory(pluginsDirectory, "WriterPlugin");

  KernelResult result = await kernel.RunAsync(
      "Hello world",
      kernel.Functions.GetFunction("WriterPlugin", "ShortPoem")
  );

  // TODO: need a way to simplify this
  // KernelResult result = await kernel.RunAsync(
  //     "WriterPlugin.ShortPoem"
  //     context: { "input": "Hello world" }
  // );

  Console.WriteLine(result.GetValue<string>());
}

// Scenario 3: Chaining functions together
//////////////////////////////////////////////////////////////////////////////////

if (false)
{
  Console.WriteLine("\n\nStarting scenario 3");
  Console.WriteLine("/////////////////////////////////////////////////////\n");


  string AzureOpenAIDeploymentName = Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!;
  string AzureOpenAIEndpoint = Env.Var("AzureOpenAI:Endpoint")!;
  string AzureOpenAIApiKey = Env.Var("AzureOpenAI:ApiKey")!;

  IKernel kernel = new KernelBuilder()
      .WithLoggerFactory(loggerFactory)
      .WithAzureChatCompletionService(
          AzureOpenAIDeploymentName,  // The name of your deployment (e.g., "gpt-35-turbo")
          AzureOpenAIEndpoint,        // The endpoint of your Azure OpenAI service
          AzureOpenAIApiKey           // The API key of your Azure OpenAI service
      )
      //.WithSemanticFunctionsFromDirectory
      .Build();

  // TODO: This should be part of the kernel builder
  kernel.ImportSemanticFunctionsFromDirectory(pluginsDirectory, "WriterPlugin");
  kernel.ImportFunctions(new TimePlugin(), "TimePlugin");

  // TODO: Create empty constructor for Plan
  Plan plan = new Plan("");
  plan.AddSteps(
      kernel.Functions.GetFunction("TimePlugin", "Today"),
      kernel.Functions.GetFunction("WriterPlugin", "ShortPoem")

  // TODO: need a way to simplify, for example:
  // "TimePlugin.Today"
  // "WriterPlugin.ShortPoem result"

  // TODO: need a way to pass variables, for example:
  // "TimePlugin.Today as | today |"
  // "WriterPlugin.ShortPoem today
  );

  // TODO: need a way to simplify this
  // KernelResult result = await kernel.RunAsync(
  //     "TimePlugin.Today"
  //     "WriterPlugin.ShortPoem"
  // );

  // TODO: need a way to pass variables, for example:
  // KernelResult result = await kernel.RunAsync(
  //     "TimePlugin.Today as | today |"
  //     "WriterPlugin.ShortPoem"
  // );


  KernelResult result = await kernel.RunAsync(plan);

  Console.WriteLine(result.GetValue<string>());
}


// Scenario 4: Testing of Handlebars based planner
//////////////////////////////////////////////////////////////////////////////////

if (false)
{
  Console.WriteLine("\n\nStarting scenario 4");
  Console.WriteLine("/////////////////////////////////////////////////////\n");


  string AzureOpenAIDeploymentName = Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!;
  string AzureOpenAIEndpoint = Env.Var("AzureOpenAI:Endpoint")!;
  string AzureOpenAIApiKey = Env.Var("AzureOpenAI:ApiKey")!;

  IKernel kernel = new KernelBuilder()
      .WithLoggerFactory(loggerFactory)
      .WithAzureChatCompletionService(
          AzureOpenAIDeploymentName,  // The name of your deployment (e.g., "gpt-35-turbo")
          AzureOpenAIEndpoint,        // The endpoint of your Azure OpenAI service
          AzureOpenAIApiKey,          // The API key of your Azure OpenAI service
          serviceId: "chat-completion"
      )
      //.WithSemanticFunctionsFromDirectory
      .Build();

  IChatCompletion chatCompletion = kernel.GetService<IChatCompletion>("chat-completion");
  ChatHistory chatMessages = chatCompletion.CreateNewChat();

  chatMessages.AddMessage(AuthorRole.System, "You are an assistant that creates Handlebar templates that can be rendered to satisfy a user's goal.");
  chatMessages.AddMessage(AuthorRole.User, "Can you write a poem about yesterday's date?");
  chatMessages.AddMessage(AuthorRole.User, "Please return the result in a JSON object with the following format:{result: <ANSWER>}");
  chatMessages.AddMessage(AuthorRole.System, @"You have the following helpers that you can use to accomplish the user's goal:
## Functions

### TimePlugin.Today
- *Description*: Returns today's date.
- *Inputs*: None.
- *Output*: 
  - `result`: date - Today's date.
- *Errors*: None expected.
- *Example*: 
  ```handlebars
  {{TimePlugin.Today}}
  ```

### TimePlugin.AddDays
- *Description*: Adds a number of days to a date.
- *Inputs*:
  - `date`: date - The date to add days to. (required)
  - `days`: number - The number of days to add; it can be negative (required)
- *Output*: 
  - `result`: date - The date with the days added.
- *Errors*: None expected.
- *Example*: 
  ```handlebars
  {{TimePlugin.AddDays date=date days=days}}
  ```
  
### WriterPlugin.ShortPoem
- *Description*: Returns a short poem.
- *Inputs*: 
  - `input`: string - What to write a poem about. (required)
- *Output*:
  - `poem`: string - The poem.
- *Errors*: None expected.
- *Example*: 
  ```handlebars
  {{WriterPlugin.ShortPoem input=input}}
  ```

Do not use helper functions that are not listed above"
  );

  chatMessages.AddMessage(AuthorRole.Assistant, "I can try to do that. Here is the JSON that you requested:");
  chatMessages.AddMessage(AuthorRole.System, "You will now provide the user with a minimal handlebar template that will accomplish their goal.");
  chatMessages.AddMessage(AuthorRole.System, "Do not provide any additional commentary or information after generating the handlebar template");

  var results = await chatCompletion.GenerateMessageAsync(chatMessages);

  Console.WriteLine(results);
}


// Scenario 5: Testing of Handlebars based planner (action oriented)
//////////////////////////////////////////////////////////////////////////////////

if (false)
{
  Console.WriteLine("\n\nStarting scenario 4");
  Console.WriteLine("/////////////////////////////////////////////////////\n");


  string AzureOpenAIDeploymentName = Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!;
  string AzureOpenAIEndpoint = Env.Var("AzureOpenAI:Endpoint")!;
  string AzureOpenAIApiKey = Env.Var("AzureOpenAI:ApiKey")!;

  IKernel kernel = new KernelBuilder()
      .WithLoggerFactory(loggerFactory)
      .WithAzureChatCompletionService(
          AzureOpenAIDeploymentName,  // The name of your deployment (e.g., "gpt-35-turbo")
          AzureOpenAIEndpoint,        // The endpoint of your Azure OpenAI service
          AzureOpenAIApiKey,          // The API key of your Azure OpenAI service
          serviceId: "chat-completion"
      )
      //.WithSemanticFunctionsFromDirectory
      .Build();

  IChatCompletion chatCompletion = kernel.GetService<IChatCompletion>("chat-completion");
  ChatHistory chatMessages = chatCompletion.CreateNewChat();

  chatMessages.AddMessage(AuthorRole.System, "You are an assistant that creates Handlebar templates that can be rendered to satisfy a user's goal.");
  chatMessages.AddMessage(AuthorRole.User, "Can you delete a task named 'Take out the trash'?");
  chatMessages.AddMessage(AuthorRole.User, "Please return the entire result in a JSON object with the following format:{result: <RESULT>}");
  chatMessages.AddMessage(AuthorRole.System, @"You have the following helpers that you can use to accomplish the user's goal:
## Functions

### ToString
- *Description*: Converts a value to a string.
- *Inputs*: 
  - `value`: any - The value to convert to a string. (required)
- *Output*: string - The string representation of the value.
- *Errors*: None expected.
- *Example*: 
  ```handlebars
  {{ToString value=value}}
  ```

### TimePlugin.Today
- *Description*: Returns today's date.
- *Inputs*: None.
- *Output*: date - Today's date.
- *Errors*: None expected.
- *Example*: 
  ```handlebars
  {{TimePlugin.Today}}
  ```

### TimePlugin.AddDays
- *Description*: Adds a number of days to a date.
- *Inputs*:
  - `date`: date - The date to add days to. (required)
  - `days`: number - The number of days to add; it can be negative (required)
- *Output*: date - The date with the days added.
- *Errors*: None expected.
- *Example*: 
  ```handlebars
  {{TimePlugin.AddDays date=date days=days}}
  ```
  
### TodoPlugin.CreateTask
- *Description*: Creates a new task.
- *Inputs*: 
  - `name`: string - The name of the task. (required)
  - `dueDate`: date - The date the task is due. (required)
  - `description`: string - The description of the task. (optional)
  - `priority`: number - The priority of the task (from 1 to 5). (optional)
- *Output*: TodoPlugin.Task - The results of the new task.
- *Errors*: None expected.
- *Example*: 
  ```handlebars
  {{TodoPlugin.CreateTask name=name dueDate=dueDate description=description priority=priority}}}
  ```

### TodoPlugin.DeleteTask
- *Description*: Deletes a task.
- *Inputs*: 
  - `id`: string - The GUID id of the task. (required)
- *Output*: TodoPlugin.Task - The results of the deleted task.
- *Errors*: None expected.
- *Example*: 
  ```handlebars
  {{TodoPlugin.DeleteTask id=id}}
  ```

### TodoPlugin.GetTask
- *Description*: Gets a task.
- *Inputs*: 
  - `id`: string - The GUID id of the task. (required)
- *Output*: TodoPlugin.Task - The results of the task.
- *Errors*: None expected.
- *Example*: 
  ```handlebars
  {{TodoPlugin.GetTask id=id}}
  ```

### TodoPlugin.UpdateTask
- *Description*: Updates a task.
- *Inputs*: 
  - `id`: string - The GUID id of the task. (required)
  - `name`: string - The name of the task. (optional)
  - `dueDate`: date - The date the task is due. (optional)
  - `description`: string - The description of the task. (optional)
  - `priority`: number - The priority of the task (from 1 to 5). (optional)
- *Output*: TodoPlugin.Task - The results of the updated task.
- *Errors*: None expected.
- *Example*: 
  ```handlebars
  {{TodoPlugin.UpdateTask id=id name=name dueDate=dueDate description=description priority=priority}}
  ```

### TodoPlugin.SearchTasks
- *Description*: Searches for tasks.
- *Inputs*: 
  - `query`: string - The query to search for. (required)
- *Output*: TodoPlugin.Task[] - The results of the search.
- *Errors*: None expected.
- *Example*: 
  ```handlebars
  {{TodoPlugin.SearchTasks query=query}}
  ```

## Types

## TodoPlugin.Task
- *Description*: A task.
- *Properties*:
    - `id`: string - The id of the task.
    - `name`: string - The name of the task.
    - `dueDate`: date - The date the task is due.
    - `description`: string - The description of the task.
    - `priority`: number - The priority of the task (from 1 to 5).


Do not use helper functions that are not listed above"
  );

  chatMessages.AddMessage(AuthorRole.System, "You will now provide the user a handlebar template that will accomplish their goal and generate the expected JSON response.");
  chatMessages.AddMessage(AuthorRole.Assistant, "I can try to do that. Here is the JSON that you requested:");
  chatMessages.AddMessage(AuthorRole.Assistant, "```");


  var results = await chatCompletion.GenerateMessageAsync(chatMessages);

  Console.WriteLine(results);
}