
using Microsoft.SemanticKernel.AI.TextCompletion;

namespace Microsoft.SemanticKernel.Handlebars;
public class LabelChoices : IModelContext<TextContent>
{
    private readonly List<TextContent>? choices;
    public Dictionary<string, object>? Properties { get; set; }

    public LabelChoices(
        List<TextContent>? choices = default,
        Dictionary<string, object>? properties = default
    )
    {
        this.choices = choices;
        Properties = properties;
    }

    public List<TextContent> GetContext()
    {
        return choices ?? new List<TextContent>();
    }
}