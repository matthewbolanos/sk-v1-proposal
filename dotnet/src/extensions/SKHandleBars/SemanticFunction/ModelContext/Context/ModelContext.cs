
namespace Microsoft.SemanticKernel.Handlebars;
public class ModelContext : IModelContext<IMessageContent>
{
    private List<IMessageContent>? context { get; set; }
    public Dictionary<string, object>? Properties { get; set; }

    public ModelContext(
        List<IMessageContent>? context = default,
        Dictionary<string, object>? properties = default
    )
    {
        this.context = context;
        Properties = properties;
    }

    public List<IMessageContent> GetContext()
    {
        return context ?? new List<IMessageContent>();
    }
}