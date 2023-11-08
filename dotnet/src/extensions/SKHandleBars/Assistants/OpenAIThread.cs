using System.Text;
using System.Text.Json;

namespace Microsoft.SemanticKernel.Handlebars;

public class OpenAIThread : IThread
{
    public string Id { get; set; }

    private readonly AssistantKernel primaryAssistant;

    private string apiKey;
    private const string _url = "https://api.openai.com/v1/threads";

    private readonly HttpClient client = new ();

    public OpenAIThread(string id, string apiKey, AssistantKernel primaryAssistant)
    {
        this.Id = id;
        this.apiKey = apiKey;
        this.primaryAssistant = primaryAssistant;
    }

    public async Task<FunctionResult> SendUserMessageAsync(string messageContent)
    {
        ModelMessage message = new ModelMessage(messageContent);
        await CreateMessageAsync(message);
        
        return await primaryAssistant.RunAsync(this);
    }
    
    public async Task CreateMessageAsync(ModelMessage message)
    {

        var requestData = new
        {
            role = message.Role,
            content = message.Content.ToString()
        };

        var url = $"{_url}/{Id}/messages";
        using var httpRequestMessage = HttpRequest.CreatePostRequest(url, requestData);

        httpRequestMessage.Headers.Add("Authorization", $"Bearer {this.apiKey}");
        httpRequestMessage.Headers.Add("OpenAI-Beta", "assistants=v1");

        using var response = await this.client.SendAsync(httpRequestMessage).ConfigureAwait(false);

        string responseBody = await response.Content.ReadAsStringAsync();
    }

    public async Task<ModelMessage> RetrieveMessageAsync(string messageId)
    {
        var url = $"{_url}/{Id}/messages/"+messageId;
        using var httpRequestMessage = HttpRequest.CreateGetRequest(url);

        httpRequestMessage.Headers.Add("Authorization", $"Bearer {this.apiKey}");
        httpRequestMessage.Headers.Add("OpenAI-Beta", "assistants=v1");

        using var response = await this.client.SendAsync(httpRequestMessage).ConfigureAwait(false);
        string responseBody = await response.Content.ReadAsStringAsync();
        ThreadMessageModel message = JsonSerializer.Deserialize<ThreadMessageModel>(responseBody);

        List<object> content = new List<object>();
        foreach(var item in message.Content)
        {
            content.Add(item.Text.Value);
        }

        return new ModelMessage(content, message.Role);
    }

    public Task<List<ModelMessage>> ListMessagesAsync()
    {
        throw new NotImplementedException();
    }
}
