
namespace Microsoft.SemanticKernel.Handlebars;
public class FunctionContent : IMessageContent
{
    public string PluginName { get; set; }
    public string Name { get; set; }
    public List<IMessageContent>? Contents { get; set; }
    public Dictionary<string, object>? Properties { get; set; }

    public FunctionContent(
        string pluginName,
        string name,
        List<IMessageContent>? contents = default,
        Dictionary<string, object>? properties = default
    )
    {
        PluginName = pluginName;
        Name = name;
        Contents = contents;
        Properties = properties;
    }
}