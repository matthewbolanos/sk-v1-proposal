

using System.Xml;

namespace Microsoft.SemanticKernel.Handlebars;
public interface IModelContextFactory
{
    public object ParseModelContext(XmlNode node, IMessageContentFactory messageContentFactory);
}