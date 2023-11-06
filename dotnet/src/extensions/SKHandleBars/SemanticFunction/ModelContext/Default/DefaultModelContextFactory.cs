

using System.Xml;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Services;

namespace Microsoft.SemanticKernel.Handlebars;
public class DefaultModelContextFactory : IModelContextFactory<ModelContext, IMessageContent>
{
    private readonly ModelContextFactory modelContextFactory = new();
    private readonly FunctionChoicesFactory functionChoicesFactory = new();
    private readonly LabelChoicesFactory labelChoicesFactory = new();

    public ModelContext ParseModelContext(XmlNode node, IMessageContentFactory<IMessageContent> messageContentFactory)
    {
        throw new NotImplementedException();
    }
}