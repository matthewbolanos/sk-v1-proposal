
using System.Xml;

namespace Microsoft.SemanticKernel.Handlebars;
public class LabelChoicesFactory : IModelContextFactory
{
    public object ParseModelContext(XmlNode contextNode, IMessageContentFactory messageContentFactory)
    {
        if (contextNode.NodeType == XmlNodeType.Element && contextNode.Name == "labels")
        {
            List<string> choices = new ();
            if (contextNode != null)
            {
                foreach (XmlNode node in contextNode.ChildNodes)
                {
                    choices.Add((string)messageContentFactory.ParseMessageContent(node));
                }
            }
            return new LabelChoices(choices);
        } else
        {
            throw new NotImplementedException();
        }

    }
}