
using System.Xml;

namespace Microsoft.SemanticKernel.Handlebars;
public class LabelChoicesFactory : IModelContextFactory<LabelChoices, TextContent>
{
    public LabelChoices ParseModelContext(XmlNode contextNode, IMessageContentFactory<TextContent> messageContentFactory)
    {
        if (contextNode.NodeType == XmlNodeType.Element && contextNode.Name == "labels")
        {
            List<TextContent> choices = new ();
            if (contextNode != null)
            {
                foreach (XmlNode node in contextNode.ChildNodes)
                {
                    choices.Add(messageContentFactory.ParseMessageContent(node));
                }
            }
            return new LabelChoices(choices);
        } else
        {
            throw new NotImplementedException();
        }

    }
}