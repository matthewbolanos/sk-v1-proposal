
namespace Microsoft.SemanticKernel.Handlebars;
public class FunctionChoices
{
    private List<FunctionContent>? choices { get; set; }
    public Dictionary<string, object>? Properties { get; set; }

    public FunctionChoices(
        List<FunctionContent>? choices = default,
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