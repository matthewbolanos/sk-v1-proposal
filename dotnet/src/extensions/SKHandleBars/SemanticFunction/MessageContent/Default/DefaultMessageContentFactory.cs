

using System.Xml;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Services;

namespace Microsoft.SemanticKernel.Handlebars;
public class DefaultMessageContentFactory : IMessageContentFactory<IMessageContent>
{
    private readonly TextContentFactory textContentFactory = new();

    public IMessageContent ParseMessageContent(XmlNode node)
    {
        IMessageContent messageContent;
        if (node.NodeType == XmlNodeType.Text)
        {
            messageContent = textContentFactory.ParseMessageContent(node);
        }
        else
        {
            throw new NotImplementedException();
        }

        return messageContent;
    }
}