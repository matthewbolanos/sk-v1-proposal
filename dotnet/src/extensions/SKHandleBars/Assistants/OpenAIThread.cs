using System.Text;
using System.Text.Json;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.AI.TextCompletion;

namespace Microsoft.SemanticKernel.Handlebars;

public class OpenAIThread : IThread
{
    public string Id { get; set; }

    public string Name { get { return Id; } }

    public string PluginName { get; }

    public string Description => throw new NotImplementedException();

    private readonly AssistantKernel primaryAssistant;

    private string apiKey;
    private const string _url = "https://api.openai.com/v1/threads";

    private readonly HttpClient client = new();

    public OpenAIThread(string id, string apiKey, AssistantKernel primaryAssistant)
    {
        this.PluginName = primaryAssistant.Name;
        this.Id = id;
        this.apiKey = apiKey;
        this.primaryAssistant = primaryAssistant;
    }

    public async Task AddUserMessageAsync(string message)
    {
        await AddMessageAsync(new ModelMessage(message));
    }

    public async Task AddMessageAsync(ModelMessage message)
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
        var url = $"{_url}/{Id}/messages/" + messageId;
        using var httpRequestMessage = HttpRequest.CreateGetRequest(url);

        httpRequestMessage.Headers.Add("Authorization", $"Bearer {this.apiKey}");
        httpRequestMessage.Headers.Add("OpenAI-Beta", "assistants=v1");

        using var response = await this.client.SendAsync(httpRequestMessage).ConfigureAwait(false);
        string responseBody = await response.Content.ReadAsStringAsync();
        ThreadMessageModel message = JsonSerializer.Deserialize<ThreadMessageModel>(responseBody);

        List<object> content = new List<object>();
        foreach (var item in message.Content)
        {
            content.Add(item.Text.Value);
        }

        return new ModelMessage(content, message.Role);
    }

    public Task<List<ModelMessage>> ListMessagesAsync()
    {
        throw new NotImplementedException();
    }

    public SemanticKernel.FunctionView Describe()
    {
        throw new NotImplementedException();
    }

    // The heart of the thread interface...
    public async Task<FunctionResult> InvokeAsync(
        IKernel kernel,
        Dictionary<string, object?> variables,
        CancellationToken cancellationToken = default,
        bool streaming = false
    )
    {
        // TODO: implement streaming so that we can pass messages back as they are created
        if (streaming)
        {
            throw new NotImplementedException();
        }

        if (kernel is AssistantKernel assistantKernel)
        {
            // Create a run on the thread
            ThreadRunModel threadRunModel = await CreateThreadRunAsync(assistantKernel, variables);
            ThreadRunStepListModel threadRunSteps;

            // Poll the run until it is complete
            while (threadRunModel.Status == "queued" || threadRunModel.Status == "in_progress" || threadRunModel.Status == "requires_action")
            {
                // Add a delay
                await Task.Delay(300);

                // If the run requires action, then we need to run the tool calls
                if (threadRunModel.Status == "requires_action")
                {
                    // Get the steps
                    threadRunSteps = await GetThreadRunStepsAsync(threadRunModel.Id);

                    // TODO: make this more efficient through parallelization
                    foreach (ThreadRunStepModel threadRunStep in threadRunSteps.Data)
                    {
                        // Retrieve all of the steps that require action
                        if (threadRunStep.Status == "in_progress" && threadRunStep.StepDetails.Type == "tool_calls")
                        {
                            // Create a list to hold the tasks
                            var tasks = new List<Task<KeyValuePair<string, object?>>>();

                            foreach (var toolCall in threadRunStep.StepDetails.ToolCalls)
                            {
                                // Create a task for each tool call
                                var task = InvokeFunctionCallAsync(kernel, toolCall.Function.Name, toolCall.Function.Arguments)
                                        .ContinueWith(t => new KeyValuePair<string, object?>(toolCall.Id, t.Result));

                                tasks.Add(task);
                            }

                            // Wait for all tasks to complete
                            var results = await Task.WhenAll(tasks);

                            // Create the dictionary from the completed tasks results
                            Dictionary<string, object?> toolCallResults = results.ToDictionary(t => t.Key, t => t.Value);

                            // Update the thread run
                            threadRunModel = await SubmitToolOutputsToRun(threadRunModel.Id, toolCallResults);
                        }
                    }
                }
                else
                {
                    threadRunModel = GetThreadRunAsync(threadRunModel.Id).Result;
                }
            }

            // Check for errors
            if (threadRunModel.Status == "failed")
            {
                return new FunctionResult(this.Name, "Ask", new List<ModelMessage>(){
                    { new ModelMessage(threadRunModel.LastError.Message) }
                });
            }

            // Get the steps
            threadRunSteps = await GetThreadRunStepsAsync(threadRunModel.Id);

            // Check step details
            List<ModelMessage> messages = new List<ModelMessage>();

            if (threadRunSteps?.Data != null)
            {
                foreach (ThreadRunStepModel threadRunStep in threadRunSteps.Data)
                {
                    // Ensure that StepDetails and MessageCreation are not null
                    if (threadRunStep?.StepDetails?.Type == "message_creation" && threadRunStep.StepDetails.MessageCreation != null)
                    {
                        // Get message Id
                        var messageId = threadRunStep.StepDetails.MessageCreation.MessageId;

                        // Ensure messageId is not null
                        if (!string.IsNullOrEmpty(messageId))
                        {
                            ModelMessage message = await this.RetrieveMessageAsync(messageId);

                            // Add the message to the list if it's not null
                            if (message != null)
                            {
                                messages.Add(message);
                            }
                        }
                    }
                }
            }
            return new FunctionResult(this.Name, this.PluginName, messages);
        }

        throw new NotImplementedException();
    }

    private async Task<string> InvokeFunctionCallAsync(IKernel kernel, string name, string arguments)
    {
        // split name
        string[] nameParts = name.Split("-");

        // get function from kernel
        var function = ((Kernel)kernel).Functions.GetFunction(nameParts[0], nameParts[1]);
        // TODO: change back to Dictionary<string, object>
        Dictionary<string, object> variables = JsonSerializer.Deserialize<Dictionary<string, object>>(arguments)!;

        var results = await kernel.RunAsync(
            function,
            variables: variables!
        );

        return results.ToString();
    }

    private async Task<ThreadRunModel> SubmitToolOutputsToRun(string runId, Dictionary<string, object?> toolCallResults)
    {
        List<object> tool_outputs = new List<object>();
        foreach(var toolCallResult in toolCallResults)
        {
            tool_outputs.Add(new
            {
                tool_call_id = toolCallResult.Key,
                output = toolCallResult.Value
            });
        }
        
        var requestData = new
        {
            tool_outputs = tool_outputs
        };

        string url = "https://api.openai.com/v1/threads/" + this.Id + "/runs/" + runId + "/submit_tool_outputs";
        using var httpRequestMessage = HttpRequest.CreatePostRequest(url, requestData);
        httpRequestMessage.Headers.Add("Authorization", $"Bearer {this.apiKey}");
        httpRequestMessage.Headers.Add("OpenAI-Beta", "assistants=v1");

        var response = await this.client.SendAsync(httpRequestMessage).ConfigureAwait(false);

        string responseBody = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ThreadRunModel>(responseBody)!;
    }

    private async Task<ThreadRunModel> CreateThreadRunAsync(AssistantKernel kernel, Dictionary<string, object?>? variables = default)
    {
        List<object> tools = new List<object>();
        if (kernel.Name != "Mathmatician")
        {
            // Much of this code was copied from the existing function calling code
            // Ideally it can reuse the same code without having to duplicate it
            foreach (FunctionView functionView in kernel.GetFunctionViews())
            {
                var OpenAIFunction = functionView.ToOpenAIFunction().ToFunctionDefinition();

                var requiredParams = new List<string>();
                var paramProperties = new Dictionary<string, object>();
                foreach (var param in functionView.Parameters)
                {
                    if (param.Type.Name == "IKernel")
                    {
                        continue;
                    }

                    // if double or int, then type is number
                    var type = param.Type.Name.ToLower();

                    if (type == "double" || type == "int" || type == "int32" || type == "int64")
                    {
                        type = "number";
                    }

                    paramProperties.Add(
                        param.Name,
                        new
                        {
                            type = type,
                            description = param.Description,
                        });

                    if (param.IsRequired ?? false)
                    {
                        requiredParams.Add(param.Name);
                    }
                }

                tools.Add(new
                {
                    type = "function",
                    function = new
                    {
                        name = OpenAIFunction.Name,
                        description = OpenAIFunction.Description,
                        parameters = new
                        {
                            type = "object",
                            properties = paramProperties,
                            required = requiredParams,
                        }
                    }
                });
            }
        }

        string assistantInstructions = variables?["instructions"]?.ToString() ?? "";

        var requestData = new
        {
            assistant_id = kernel.Id,
            instructions = assistantInstructions,
            tools = tools
        };

        string requestDataJson = JsonSerializer.Serialize(requestData);

        string url = "https://api.openai.com/v1/threads/" + this.Id + "/runs";
        using var httpRequestMessage = HttpRequest.CreatePostRequest(url, requestData);
        httpRequestMessage.Headers.Add("Authorization", $"Bearer {this.apiKey}");
        httpRequestMessage.Headers.Add("OpenAI-Beta", "assistants=v1");

        var response = await this.client.SendAsync(httpRequestMessage).ConfigureAwait(false);

        string responseBody = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ThreadRunModel>(responseBody)!;
    }

    private async Task<ThreadRunModel> GetThreadRunAsync(string runId)
    {
        string url = "https://api.openai.com/v1/threads/" + this.Id + "/runs/" + runId;
        using var httpRequestMessage2 = HttpRequest.CreateGetRequest(url);

        httpRequestMessage2.Headers.Add("Authorization", $"Bearer {this.apiKey}");
        httpRequestMessage2.Headers.Add("OpenAI-Beta", "assistants=v1");

        var response = await this.client.SendAsync(httpRequestMessage2).ConfigureAwait(false);

        string responseBody = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ThreadRunModel>(responseBody)!;
    }

    private async Task<ThreadRunStepListModel> GetThreadRunStepsAsync(string runId)
    {
        string url = "https://api.openai.com/v1/threads/" + this.Id + "/runs/" + runId + "/steps";
        using var httpRequestMessage = HttpRequest.CreateGetRequest(url);

        httpRequestMessage.Headers.Add("Authorization", $"Bearer {this.apiKey}");
        httpRequestMessage.Headers.Add("OpenAI-Beta", "assistants=v1");

        var response = await this.client.SendAsync(httpRequestMessage).ConfigureAwait(false);
        string responseBody = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ThreadRunStepListModel>(responseBody)!;
    }


    public Task<Orchestration.FunctionResult> InvokeAsync(Orchestration.SKContext context, AIRequestSettings? requestSettings = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ISKFunction SetAIService(Func<ITextCompletion> serviceFactory)
    {
        throw new NotImplementedException();
    }

    public ISKFunction SetAIConfiguration(AIRequestSettings? requestSettings)
    {
        throw new NotImplementedException();
    }

    public ISKFunction SetDefaultSkillCollection(IReadOnlyFunctionCollection skills)
    {
        throw new NotImplementedException();
    }

    public ISKFunction SetDefaultFunctionCollection(IReadOnlyFunctionCollection functions)
    {
        throw new NotImplementedException();
    }

    public AIRequestSettings? RequestSettings => throw new NotImplementedException();

    public string SkillName => throw new NotImplementedException();

    public bool IsSemantic => throw new NotImplementedException();
}
