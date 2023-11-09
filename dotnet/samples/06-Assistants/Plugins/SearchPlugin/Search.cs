// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Plugins.Web.Bing;

public sealed class Search
{
    private readonly BingConnector _bingConnector;

    public Search(string apiKey, ILoggerFactory? loggerFactory = null) :
        this(apiKey, new HttpClient(), loggerFactory)
    {

    }

    public Search(string apiKey, HttpClient httpClient, ILoggerFactory? loggerFactory = null)
    {
        this._bingConnector = new BingConnector(apiKey, httpClient, loggerFactory);
    }


    [SKFunction, Description("Searches Bing for the given query")]
    public async Task<string> SearchAsync(
        [Description("The search query"), SKName("query")] string query
    )
    {
        var results = await this._bingConnector.SearchAsync(query, 10);

        return JsonSerializer.Serialize(results.ToList());
    }
}


[TypeConverter(typeof(SearchResultsConverter))]
public class SearchResults {
    public List<string> Results { get; set; } = new List<string>();
}

public class SearchResultsConverter : TypeConverter
{
    /// <inheritdoc/>
    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        return destinationType == typeof(string) || base.CanConvertTo(context, destinationType) ||
            (destinationType == typeof(SearchResults));
    }

    /// <inheritdoc/>
    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        if (value is SearchResults response && destinationType == typeof(string))
        {
            // Turn value into a string using JSON
            string json = JsonSerializer.Serialize(response);
            return json;
        }
        if (value is string str && destinationType == typeof(string))
        {
            // Convert string to SearchResults
            return JsonSerializer.Deserialize<SearchResults>(str);
        }

        return base.ConvertTo(context, culture, value, destinationType);
    }
}
