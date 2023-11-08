namespace Microsoft.SemanticKernel.Handlebars;

public interface IThread
{
    public string Id { get; set; }
    
    public Task<FunctionResult> SendUserMessageAsync(string messageContent);

    public Task CreateMessageAsync(ModelMessage message);

    public Task<ModelMessage> RetrieveMessageAsync(string messageId);

    public Task<List<ModelMessage>> ListMessagesAsync();
}
