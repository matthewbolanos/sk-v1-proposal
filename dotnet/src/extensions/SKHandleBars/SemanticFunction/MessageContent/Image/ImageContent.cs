
using System.Text.RegularExpressions;

namespace Microsoft.SemanticKernel.Handlebars;
public class ImageContent
{
    public Uri ImageUri { get; set; }
    public byte[]? ImageData { get; set; }
    public string? MimeType { get; set; }
    public Dictionary<string, object>? Properties { get; set; }

    public ImageContent(string image, Dictionary<string, object>? properties = default)
    {
        // Check if it's a data URI or a URL
        if (image.StartsWith("data:"))
        {
            // Extract MIME type and base64 data using a regular expression
            var match = Regex.Match(image, @"data:(?<type>.+?);base64,(?<data>.+)");

            if (!match.Success)
            {
                throw new ArgumentException("Invalid data URI format", nameof(image));
            }

            MimeType = match.Groups["type"].Value;
            var base64Data = match.Groups["data"].Value;

            // Convert the base64 string to a byte array
            try
            {
                ImageData = Convert.FromBase64String(base64Data);
            }
            catch (FormatException)
            {
                throw new ArgumentException("Invalid base64 string", nameof(image));
            }
            
            Properties = properties;
        }
        else
        {
            // Assume it's a URL
            try
            {
                ImageUri = new Uri(image);
            }
            catch (UriFormatException ex)
            {
                throw new ArgumentException("Invalid URL format", nameof(image), ex);
            }
            Properties = properties;
        }
    }

    public string GetBase64SrcString()
    {
        if (ImageData == null)
        {
            throw new InvalidOperationException("Image data is not available");
        }

        return $"data:{MimeType};base64,{Convert.ToBase64String(ImageData)}";
    }

    public string GetSrc()
    {
        if (ImageUri != null)
        {
            return ImageUri.ToString();
        }
        else if (ImageData != null)
        {
            return GetBase64SrcString();
        }

        throw new InvalidOperationException("Image data is not available");
    }

    public override string ToString()
    {
        return $"Image: {GetSrc()}";
    }
}