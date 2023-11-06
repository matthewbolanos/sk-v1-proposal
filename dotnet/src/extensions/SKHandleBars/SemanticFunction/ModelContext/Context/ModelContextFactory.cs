
using System.Xml;

namespace Microsoft.SemanticKernel.Handlebars;
public class ModelContextFactory : IModelContextFactory<ModelContext, IMessageContent>
{
    public ModelContext ParseModelContext(XmlNode contextNode, IMessageContentFactory<IMessageContent> messageContentFactory)
    {
        if (contextNode.NodeType == XmlNodeType.Element && contextNode.Name == "context")
        {
            List<IMessageContent> messageParts = new ();
            if (contextNode != null)
            {
                foreach (XmlNode node in contextNode.ChildNodes)
                {
                    messageParts.Add(messageContentFactory.ParseMessageContent(node));
                }
            }
            return new ModelContext(messageParts);
        } else
        {
            throw new NotImplementedException();
        }

    }
}