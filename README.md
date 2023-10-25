# Samples for Semantic Kernel v1 proposal

This repo demonstrates what AI apps may look like once v1 of Semantic Kernel is complete.

## Prerequisites

- [.NET 6](https://dotnet.microsoft.com/download/dotnet/6.0) is required to run this sample.
- Install the recommended extensions
- [C#](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp)
- [Semantic Kernel Tools](https://marketplace.visualstudio.com/items?itemName=ms-semantic-kernel.semantic-kernel) (optional)

## Configuring the sample


Before runnning the samples, you must first configure an Azure OpenAI endpoint using the following commands.

Navigate to one of the samples, e.g., `cd sk-v1-proposal/samples/01-SimpleChat`.

Set your user secrets.

```
dotnet user-secrets set "AzureOpenAI:DeploymentType" "chat-completion"
dotnet user-secrets set "AzureOpenAI:ChatCompletionDeploymentName" "gpt-35-turbo"
dotnet user-secrets set "AzureOpenAI:Endpoint" "... your Azure OpenAI endpoint ..."
dotnet user-secrets set "AzureOpenAI:ApiKey" "... your Azure OpenAI key ..."
dotnet user-secrets set ""Bing:ApiKey" "... your Bing key ..."

```
