
namespace Microsoft.SemanticKernel.Handlebars;
public class ModelContext
{
    private List<object>? context { get; set; }
    public Dictionary<string, object>? Properties { get; set; }

    public ModelContext(
        List<object>? context = default,
        Dictionary<string, object>? properties = default
    )
    {
        this.context = context;
        Properties = properties;
    }

    public List<object> GetContext()
    {
        return context ?? new List<object>();
    }

    public override string ToString()
    {
        return string.Join("", GetContext());
    }
}