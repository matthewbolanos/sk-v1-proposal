
namespace Microsoft.SemanticKernel.Handlebars;
public class FunctionContent
{
    public string PluginName { get; set; }
    public string Name { get; set; }
    public List<object>? Contents { get; set; }
    public Dictionary<string, object>? Properties { get; set; }

    public FunctionContent(
        string pluginName,
        string name,
        List<object>? contents = default,
        Dictionary<string, object>? properties = default
    )
    {
        PluginName = pluginName;
        Name = name;
        Contents = contents;
        Properties = properties;
    }
}