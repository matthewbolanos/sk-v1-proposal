
using System.Xml;

namespace Microsoft.SemanticKernel.Handlebars;
public class ImageContentFactory : IMessageContentFactory
{
    public object ParseMessageContent(XmlNode node)
    {
        ImageContent messageContent;
        if (node.NodeType == XmlNodeType.Element && node.Name == "img")
        {
            XmlElement element = (XmlElement)node;
            string src = element.GetAttribute("src");
            messageContent = new ImageContent(src);
        } else
        {
            throw new NotImplementedException();
        }

        return messageContent;
    }
}