using System.Text.Json.Serialization;

namespace Microsoft.SemanticKernel.Handlebars;

public class AskResponse
{
    [JsonPropertyName("thread_id")]
    public string ThreadId { get; set; }

    [JsonPropertyName("response")]
    public string Response { get; set; }

    [JsonPropertyName("system_instructions")]
    public string Instructions { get; set; }
}