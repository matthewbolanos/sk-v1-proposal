namespace Microsoft.SemanticKernel.Handlebars;

public interface IThread : ISKFunction
{
    public string Id { get; set; }
    public Task AddUserMessageAsync(string message);
    public Task AddMessageAsync(ModelMessage message);
    public Task<ModelMessage> RetrieveMessageAsync(string messageId);
    public Task<List<ModelMessage>> ListMessagesAsync();
}
