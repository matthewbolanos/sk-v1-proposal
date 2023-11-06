
using System.Xml;

namespace Microsoft.SemanticKernel.Handlebars;
public class FunctionChoicesFactory : IModelContextFactory<FunctionChoices, FunctionContent>
{
    public FunctionChoices ParseModelContext(XmlNode contextNode, IMessageContentFactory<FunctionContent> messageContentFactory)
    {
        if (contextNode.NodeType == XmlNodeType.Element && contextNode.Name == "functions")
        {
            List<FunctionContent> choices = new ();
            if (contextNode != null)
            {
                foreach (XmlNode node in contextNode.ChildNodes)
                {
                    choices.Add(messageContentFactory.ParseMessageContent(node));
                }
            }
            return new FunctionChoices(choices);
        } else
        {
            throw new NotImplementedException();
        }

    }
}