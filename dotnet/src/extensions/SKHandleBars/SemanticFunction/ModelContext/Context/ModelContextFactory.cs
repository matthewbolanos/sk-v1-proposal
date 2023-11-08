
using System.Xml;

namespace Microsoft.SemanticKernel.Handlebars;
public class ModelContextFactory : IModelContextFactory
{
    public object ParseModelContext(XmlNode contextNode, IMessageContentFactory messageContentFactory)
    {
        if (contextNode.NodeType == XmlNodeType.Element && contextNode.Name == "context")
        {
            List<object> messageParts = new ();
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