

using Azure;
using Azure.AI.OpenAI;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Services;

namespace Microsoft.SemanticKernel.Handlebars;
public class ModelRequest 
{
    public List<ModelMessage>? Messages { get; set; }
    public Dictionary<string, object>? Contexts { get; set; }
    
    public Dictionary<string, object>? Properties { get; set; }

    public ModelRequest(
        List<ModelMessage>? messages = default,
        Dictionary<string, object>? contexts = default,
        Dictionary<string, object>? properties = default
    )
    {
        Messages = messages;
        Contexts = contexts;
        Properties = properties;
    }
}