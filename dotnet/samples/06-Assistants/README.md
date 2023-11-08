# Multi-modal sample

This sample demonstrates how to use other models with Semantic Kernel.

## Prerequisites

- [.NET 6](https://dotnet.microsoft.com/download/dotnet/6.0) is required to run this sample.
- Install the recommended extensions
- [C#](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp)
- [Semantic Kernel Tools](https://marketplace.visualstudio.com/items?itemName=ms-semantic-kernel.semantic-kernel) (optional)

## Configuring the sample

Configure an Azure OpenAI endpoint

```
cd ./dotnet/samples/06-Assistants

dotnet user-secrets set "AzureOpenAI:Gpt35TurboDeploymentName" "gpt-35-turbo"
dotnet user-secrets set "AzureOpenAI:Gpt4DeploymentName" "gpt-4"
dotnet user-secrets set "AzureOpenAI:Endpoint" "... your Azure OpenAI endpoint ..."
dotnet user-secrets set "AzureOpenAI:ApiKey" "... your Azure OpenAI key ..."
dotnet user-secrets set "OpenAI:ApiKey" "... your OpenAI key ..."
dotnet user-secrets set ""Bing:ApiKey" "... your Bing key ..."
dotnet user-secrets set "HuggingFace:ApiKey" "... your Hugging Face key ..."
dotnet user-secrets set "HuggingFace:FillMaskTaskEndpoint" "... your Hugging Face endpoint ..."
dotnet user-secrets set "HuggingFace:QuestionAnsweringTaskEndpoint" "... your Hugging Face endpoint ..."
dotnet user-secrets set "HuggingFace:SummarizationTaskEndpoint" "... your Hugging Face endpoint ..."
dotnet user-secrets set "HuggingFace:TextToImageTaskEndpoint" "... your Hugging Face endpoint ..."
```

## Running the sample

```
cd ./dotnet/samples/06-Assistants

dotnet run
```