

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
    public List<IModelContext<IMessageContent>>? Context { get; set; }
    
    public Dictionary<string, object>? Properties { get; set; }

    public ModelRequest(
        List<ModelMessage>? messages = default,
        List<IModelContext<IMessageContent>>? context = default,
        Dictionary<string, object>? properties = default
    )
    {
        Messages = messages;
        Context = context;
        Properties = properties;
    }
}