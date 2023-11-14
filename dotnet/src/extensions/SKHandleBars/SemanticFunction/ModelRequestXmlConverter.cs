

using System.Xml;

namespace Microsoft.SemanticKernel.Handlebars;

public class ModelRequestXmlConverter
{
    private readonly IMessageContentFactory messageContentFactory;
    private readonly IModelContextFactory modelContextFactory;

    public ModelRequestXmlConverter(
        IMessageContentFactory messageContentFactory,
        IModelContextFactory modelContextFactory
    )
    {
        this.messageContentFactory = messageContentFactory;
        this.modelContextFactory = modelContextFactory;
    }

    public ModelRequestXmlConverter()
    {
        this.messageContentFactory = new DefaultMessageContentFactory();
        this.modelContextFactory = new DefaultModelContextFactory();
    }

    public ModelMessage ParseModelMessage(XmlNode messageNode)
    {
        if (messageNode.NodeType == XmlNodeType.Element && messageNode != null)
        {
            XmlElement messageElement = (XmlElement)messageNode;
            MessageParts messageParts = new();
            foreach (XmlNode node in messageNode.ChildNodes)
            {
                messageParts.Add(messageContentFactory.ParseMessageContent(node));
            }

            string role = messageElement.GetAttribute("role");
            return new ModelMessage(messageParts, role);
        }

        throw new NotImplementedException();
    }

    public ModelRequest ParseXml(string xml, string defaultRole = "user")
    {
        XmlDocument xmlDoc = new();
        xmlDoc.LoadXml(xml);
        
        List<ModelMessage> modelMessages = new();
        Dictionary<string, object> modelContext = new();
        XmlNode? root = xmlDoc.DocumentElement;

        // Check if the root just has one text node
        if (root != null && root.ChildNodes.Count == 1 && root.FirstChild?.NodeType == XmlNodeType.Text)
        {
            string text = root.FirstChild.Value!;
            modelMessages.Add(new ModelMessage(text, defaultRole));
            return new ModelRequest(modelMessages, modelContext);
        }

        // Check if the root is not null
        if (root != null)
        {
            XmlNodeList childNodes = root.ChildNodes;

            foreach (XmlNode node in childNodes)
            {
                if (node.NodeType == XmlNodeType.Element && node.Name == "message")
                {
                    ModelMessage modelMessage = ParseModelMessage(node);
                    modelMessages.Add(modelMessage);
                } else
                {
                    modelContext.Add(node.Name, modelContextFactory.ParseModelContext(node, messageContentFactory));
                }
            }
        }
        return new ModelRequest(modelMessages, modelContext);
    }
}