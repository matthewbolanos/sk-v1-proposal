
using System.Xml;

namespace Microsoft.SemanticKernel.Handlebars;
public class TextContentFactory : IMessageContentFactory<TextContent>
{
    public TextContent ParseMessageContent(XmlNode node)
    {
        TextContent messageContent;
        if (node.NodeType == XmlNodeType.Text)
        {
            messageContent = new TextContent(node.Value!);
        }
        else
        {
            throw new NotImplementedException();
        }

        return messageContent;
    }
}