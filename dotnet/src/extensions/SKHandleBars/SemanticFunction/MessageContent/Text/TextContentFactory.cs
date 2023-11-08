
using System.Xml;

namespace Microsoft.SemanticKernel.Handlebars;
public class TextContentFactory : IMessageContentFactory
{
    public object ParseMessageContent(XmlNode node)
    {
        string messageContent;
        if (node.NodeType == XmlNodeType.Text)
        {
            messageContent = node.Value!.Trim();
        }
        else
        {
            throw new NotImplementedException();
        }

        return messageContent;
    }
}