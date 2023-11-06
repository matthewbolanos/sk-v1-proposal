
using System.Xml;

namespace Microsoft.SemanticKernel.Handlebars;
public class ImageContentFactory : IMessageContentFactory<ImageContent>
{
    public ImageContent ParseMessageContent(XmlNode node)
    {
        ImageContent messageContent;
        if (node.NodeType == XmlNodeType.Element && node.Name == "img")
        {
            XmlElement element = (XmlElement)node;
            string src = element.GetAttribute("src");
            messageContent = new ImageContent(new (src));
        } else
        {
            throw new NotImplementedException();
        }

        return messageContent;
    }
}