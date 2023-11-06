
namespace Microsoft.SemanticKernel.Handlebars;
public class ModelMessage
{
    public string Role { get; set; }
    public object Content { get; set; }
    public Dictionary<string, object>? Properties { get; set; }

    public ModelMessage(object content, string role = "user", Dictionary<string, object>? properties = default)
    {
        Role = role;
        Content = content;
        Properties = properties;
    }
}