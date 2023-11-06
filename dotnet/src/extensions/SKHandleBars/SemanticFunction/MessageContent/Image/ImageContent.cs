
namespace Microsoft.SemanticKernel.Handlebars;
public class ImageContent : IMessageContent
{
    public Uri ImageUri { get; set; }
    public string? MimeType { get; set; }
    public Dictionary<string, object>? Properties { get; set; }

    public ImageContent(Uri imageUri, string? mimeType = default, Dictionary<string, object>? properties = default)
    {
        ImageUri = imageUri;
        MimeType = mimeType;
        Properties = properties;
    }
}