
using System.Collections;
using System.Text;

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

    public override string ToString()
    {
        if (Content is IEnumerable enumerable)
        {
            var sb = new StringBuilder();
            foreach (var item in enumerable)
            {
                sb.Append(item);
            }
            return sb.ToString();
        }
        return Content.ToString() ?? string.Empty;
    }
}