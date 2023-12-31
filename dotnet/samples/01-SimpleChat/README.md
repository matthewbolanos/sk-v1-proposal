# Simple chat

This sample demonstrates how to create a simple chat bot using V1 of Semantic Kernel.

## Prerequisites

- [.NET 6](https://dotnet.microsoft.com/download/dotnet/6.0) is required to run this sample.
- Install the recommended extensions
- [C#](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp)
- [Semantic Kernel Tools](https://marketplace.visualstudio.com/items?itemName=ms-semantic-kernel.semantic-kernel) (optional)

## Configuring the sample

Configure an Azure OpenAI endpoint

```
cd ./dotnet/samples/01-SimpleChat

dotnet user-secrets set "AzureOpenAI:Gpt35TurboDeploymentName" "gpt-35-turbo"
dotnet user-secrets set "AzureOpenAI:Gpt4DeploymentName" "gpt-4"
dotnet user-secrets set "AzureOpenAI:Endpoint" "... your Azure OpenAI endpoint ..."
dotnet user-secrets set "AzureOpenAI:ApiKey" "... your Azure OpenAI key ..."
```

## Running the sample

```
cd ./dotnet/samples/01-SimpleChat

dotnet run
```