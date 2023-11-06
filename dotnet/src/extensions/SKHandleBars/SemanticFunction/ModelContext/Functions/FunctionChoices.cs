
namespace Microsoft.SemanticKernel.Handlebars;
public class FunctionChoices : IModelContext<FunctionContent>
{
    public List<FunctionContent>? Choices { get; set; }
    public Dictionary<string, object>? Properties { get; set; }

    public FunctionChoices(
        List<FunctionContent>? choices = default,
        Dictionary<string, object>? properties = default
    )
    {
        Choices = choices;
        Properties = properties;
    }

    public List<FunctionContent> GetContext()
    {
        throw new NotImplementedException();
    }
}