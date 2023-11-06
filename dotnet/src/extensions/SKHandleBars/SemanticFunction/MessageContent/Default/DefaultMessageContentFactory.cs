

using System.Xml;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Services;

namespace Microsoft.SemanticKernel.Handlebars;
public class DefaultMessageContentFactory : IMessageContentFactory
{
    private readonly TextContentFactory textContentFactory = new();
    private readonly ImageContentFactory imageContentFactory = new();
    private readonly FunctionContentFactory functionContentFactory = new();

    public object ParseMessageContent(XmlNode node)
    {
        object messageContent;
        if (node.NodeType == XmlNodeType.Text)
        {
            messageContent = textContentFactory.ParseMessageContent(node);
        } else if (node.NodeType == XmlNodeType.Element && node.Name == "img")
        {
            messageContent = imageContentFactory.ParseMessageContent(node);
        } else if (node.NodeType == XmlNodeType.Element && node.Name == "function")
        {
            messageContent = functionContentFactory.ParseMessageContent(node);
        } else
        {
            throw new NotImplementedException();
        }

        return messageContent;
    }
}