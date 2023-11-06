

using System.Xml;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Services;

namespace Microsoft.SemanticKernel.Handlebars;
public class DefaultModelContextFactory : IModelContextFactory
{
    private readonly ModelContextFactory modelContextFactory = new();
    private readonly FunctionChoicesFactory functionChoicesFactory = new();
    private readonly LabelChoicesFactory labelChoicesFactory = new();

    public object ParseModelContext(XmlNode node, IMessageContentFactory messageContentFactory)
    {
        object modelContext;
         if (node.NodeType == XmlNodeType.Element && node.Name == "context")
        {
            modelContext = modelContextFactory.ParseModelContext(node, messageContentFactory);
        } else if (node.NodeType == XmlNodeType.Element && node.Name == "functions")
        {
            modelContext = functionChoicesFactory.ParseModelContext(node, messageContentFactory);
        } else if (node.NodeType == XmlNodeType.Element && node.Name == "labels")
        {
            modelContext = labelChoicesFactory.ParseModelContext(node, messageContentFactory);
        } else
        {
            throw new NotImplementedException();
        }

        return modelContext;
    }
}