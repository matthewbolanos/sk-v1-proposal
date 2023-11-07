
using Microsoft.SemanticKernel.AI.TextCompletion;

namespace Microsoft.SemanticKernel.Handlebars;
public class LabelChoices
{
    private readonly List<string>? choices;
    public Dictionary<string, object>? Properties { get; set; }

    public LabelChoices(
        List<string>? choices = default,
        Dictionary<string, object>? properties = default
    )
    {
        this.choices = choices;
        Properties = properties;
    }

    public List<object> GetContext()
    {
        return choices?.Select(choice => (object)choice).ToList() ?? new List<object>();
    }
}