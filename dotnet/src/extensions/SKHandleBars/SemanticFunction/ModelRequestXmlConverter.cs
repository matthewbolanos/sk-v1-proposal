

using System.Xml;
using Microsoft.SemanticKernel.AI.ChatCompletion;

namespace Microsoft.SemanticKernel.Handlebars;

public class XmlToObjectConverter
{
    private readonly IMessageContentFactory<IMessageContent> messageContentFactory;
    private readonly IModelContextFactory<ModelContext, IMessageContent> modelContextFactory;

    public XmlToObjectConverter(
        IMessageContentFactory<IMessageContent> messageContentFactory,
        IModelContextFactory<ModelContext, IMessageContent> modelContextFactory
    )
    {
        this.messageContentFactory = messageContentFactory;
        this.modelContextFactory = modelContextFactory;
    }

    public XmlToObjectConverter()
    {
        this.messageContentFactory = new DefaultMessageContentFactory();
        this.modelContextFactory = new DefaultModelContextFactory();
    }

    public ModelMessage ParseModelMessage(XmlNode messageNode)
    {
        if (messageNode.NodeType == XmlNodeType.Element && messageNode != null)
        {
            XmlElement messageElement = (XmlElement)messageNode;
            List<IMessageContent> messageParts = new();
            foreach (XmlNode node in messageNode.ChildNodes)
            {
                messageParts.Add(messageContentFactory.ParseMessageContent(node));
            }

            string role = messageElement.GetAttribute("role");
            return new ModelMessage(messageParts, role);
        }

        throw new NotImplementedException();
    }

    public ModelRequest ParseXml(string xml)
    {
        XmlDocument xmlDoc = new();
        xmlDoc.LoadXml(xml);
        
        List<ModelMessage> modelMessages = new();
        XmlNodeList messageNodes = xmlDoc.DocumentElement!.SelectNodes("//message")!;

        foreach (XmlNode messageNode in messageNodes)
        {
            try
            {
                ModelMessage modelMessage = ParseModelMessage(messageNode);
                modelMessages.Add(modelMessage);
            }
            catch (NotImplementedException e)
            {
                throw new NotImplementedException(e.Message);
            }
        }

        List<IModelContext<IMessageContent>> modelContext = new();
        XmlNodeList contextNodes = xmlDoc.DocumentElement.SelectNodes("//*[not(self::message)]")!;

        foreach (XmlNode node in contextNodes)
        {
            modelContext.Add(modelContextFactory.ParseModelContext(node, messageContentFactory));
        }
        return new ModelRequest(modelMessages, modelContext);
    }
}