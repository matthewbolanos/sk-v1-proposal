
namespace Microsoft.SemanticKernel.Handlebars;
public class TextContent : IMessageContent
{
    public string Text { get; set; }

    public TextContent(string text)
    {
        Text = text;
    }

    public override string ToString()
    {
        return Text;
    }
}