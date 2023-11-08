
using System.Xml;

namespace Microsoft.SemanticKernel.Handlebars;
public class FunctionChoicesFactory : IModelContextFactory
{
    public object ParseModelContext(XmlNode contextNode, IMessageContentFactory messageContentFactory)
    {
        if (contextNode.NodeType == XmlNodeType.Element && contextNode.Name == "functions")
        {
            List<FunctionContent> choices = new ();
            if (contextNode != null)
            {
                foreach (XmlNode node in contextNode.ChildNodes)
                {
                    choices.Add((FunctionContent)messageContentFactory.ParseMessageContent(node));
                }
            }
            return new FunctionChoices(choices);
        } else
        {
            throw new NotImplementedException();
        }

    }
}