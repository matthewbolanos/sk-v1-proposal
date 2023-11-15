

using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Azure.AI.OpenAI;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.ChatCompletion;

namespace Microsoft.SemanticKernel.Handlebars;
public class OllamaGeneration : AIService
{
    private readonly Connectors.AI.OpenAI.ChatCompletion.OpenAIChatCompletion azureChatCompletion;
    private readonly ModelRequestXmlConverter modelRequestXmlConverter = new();

    private readonly HttpClient httpClient = new HttpClient();

    private const string endpoint = "http://localhost:11434/api/generate";

    public OllamaGeneration(string modelId): base(modelId)
    {
    }

    public async override Task<FunctionResult> GetModelResultAsync(IKernel kernel, string pluginName, string name, string prompt, Dictionary<object, BinaryFile>? files = default)
    {
        string chatHistory = this.OllamaPromptFromPrompt(prompt);

        object request = new
        {
            model = this.ModelId,
            prompt = chatHistory,
            // raw = true,
            stream = false,
            options = new {
                stop = new List<string>(){
                    "</s>"
                }
            }
        };

        var httpRequestMessage = HttpRequest.CreatePostRequest(endpoint, request);
        var response = await httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);
        string responseBody = await response.Content.ReadAsStringAsync();
        var deserializedResults = JsonSerializer.Deserialize<OllamaResponseModel>(responseBody);
        var result = new FunctionResult(name, pluginName, deserializedResults.Response);
        result.Metadata.Add(AIFunctionResultExtensions.ModelResultsMetadataKey, deserializedResults);

        return result;
    }

    public async override Task<FunctionResult> GetModelStreamingResultAsync(IKernel kernel, string pluginName, string name, string prompt, Dictionary<object, BinaryFile>? files = default)
    {
        throw new NotImplementedException();
    }

    private string OllamaPromptFromPrompt(string prompt)
    {
        ModelRequest modelRequest = modelRequestXmlConverter.ParseXml(prompt);

        StringBuilder ollamaPrompt = new StringBuilder();
        int i = 0;

        string? activeRole = null;

        while (i < modelRequest.Messages!.Count)
        {
            if (activeRole == null)
            {
                // ollamaPrompt.Append("<s>");
            }
            if (activeRole == null && (modelRequest.Messages[i].Role == "user" || modelRequest.Messages[i].Role == "system"))
            {
                // ollamaPrompt.Append("[INST] ");
            }

            if (modelRequest.Messages[i].Role == "system")
            {
                if (activeRole == "system")
                {
                    ollamaPrompt.Append(' ');
                }
                if (activeRole == "user")
                {
                    // ollamaPrompt.Append(" [/INST] ");
                    // ollamaPrompt.Append("</s>");
                    // ollamaPrompt.Append("<s>");
                    // ollamaPrompt.Append("[INST] ");
                }
                if (activeRole == "assistant")
                {
                    // ollamaPrompt.Append("</s>");
                    // ollamaPrompt.Append("<s>");
                    // ollamaPrompt.Append("[INST] ");
                }
                // ollamaPrompt.Append("<<SYS>>\n");
                ollamaPrompt.Append(modelRequest.Messages[i].Content);
                // ollamaPrompt.Append("\n<<SYS>>\n\n\n");
                activeRole = "system";
                i++;
                continue;
            }
            if (modelRequest.Messages[i].Role == "user")
            {
                if (activeRole == "user")
                {
                    ollamaPrompt.Append(' ');
                }
                if (activeRole == "assistant")
                {
                    // ollamaPrompt.Append("</s>");
                    // ollamaPrompt.Append("<s>");
                    // ollamaPrompt.Append("[INST] ");
                }
                ollamaPrompt.Append(modelRequest.Messages[i].Content);
                activeRole = "user";
                i++;
                continue;
            }
            if (modelRequest.Messages[i].Role == "assistant")
            {
                if (activeRole == "system")
                {
                    // ollamaPrompt.Append(" [/INST] ");
                }
                if (activeRole == "user")
                {
                    // ollamaPrompt.Append(" [/INST] ");
                }
                if (activeRole == "assistant")
                {
                    ollamaPrompt.Append(' ');
                }
                ollamaPrompt.Append(modelRequest.Messages[i].Content);
                activeRole = "assistant";
                i++;
                continue;
            }
        }

        if (activeRole == "system")
        {
            // ollamaPrompt.Append(" [/INST] ");
        }
        if (activeRole == "user")
        {
            // ollamaPrompt.Append(" [/INST] ");
        }
        // ollamaPrompt.Append("</s>");

        return ollamaPrompt.ToString();
}

    public override List<Type> OutputTypes()
    {
        throw new NotImplementedException();
    }

    public override List<string> Capabilities()
    {
        throw new NotImplementedException();
    }
}
