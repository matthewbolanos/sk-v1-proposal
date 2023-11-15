

using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Events;
using Microsoft.SemanticKernel.Http;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Services;
using Microsoft.SemanticKernel.TemplateEngine;
using YamlDotNet.Serialization;

namespace Microsoft.SemanticKernel.Handlebars;

public class AssistantKernel : Kernel, IPlugin
{

	public string Id { get; private set; }
	public string Name { get; }

    public string? Description { get; }
		
    private string? instructions { get; }

    IEnumerable<ISKFunction> IPlugin.Functions {
		get { return this.functions; }
	}

	private HttpClient client = new HttpClient();

	private readonly string apiKey;

	private readonly string model;

	private readonly List<ISKFunction> functions;

    private readonly ModelRequestXmlConverter modelRequestXmlConverter = new();

	private readonly List<VariableViewModel> inputs;

	// Allows the creation of an assistant from a YAML file
	public static AssistantKernel FromConfiguration(
		string configurationFile,
		List<IAIService>? aiServices = null,
		List<IPlugin>? plugins = null,
		List<IPromptTemplateEngine>? promptTemplateEngines = null
	)
	{
		// Open the YAML configuration file
        var yamlContent = File.ReadAllText(configurationFile);
        var deserializer = new DeserializerBuilder()
            .WithTypeConverter(new ExecutionSettingsModelConverter())
            .Build();
        AssistantKernelModel assistantKernelModel = deserializer.Deserialize<AssistantKernelModel>(yamlContent);

		// Create the assistant kernel
		return new AssistantKernel(
			assistantKernelModel.Name,
			assistantKernelModel.Description,
			assistantKernelModel.Template,
			aiServices,
			plugins,
			assistantKernelModel.InputVariables,
			promptTemplateEngines
		);
	}

	public AssistantKernel(
		string name,
		string? description,
		string? instructions,
		List<IAIService>? aiServices = null,
		List<IPlugin>? plugins = null,
		List<VariableViewModel>? inputs = null,
		List<IPromptTemplateEngine>? promptTemplateEngines = null
	) : base(aiServices, plugins, promptTemplateEngines)
	{
		this.Name = name;
		this.Description = description;
		this.instructions = instructions;
		this.inputs = inputs ?? new List<VariableViewModel>();
		
		// Grab the first AI service for the apiKey and model for the Assistants API
		// This requires that the API key be made internal so it can be accessed here
		this.apiKey = ((OpenAIChatCompletion)this.AIServices[0]).ApiKey;
		this.model = ((OpenAIChatCompletion)this.AIServices[0]).ModelId;

		// Create an AI service provider using the AI services
		AIServiceCollection services = new AIServiceCollection();
		Dictionary<Type, string> defaultIds = new(){};

		if (aiServices != null)
		{
			foreach (IAIService aiService in aiServices)
			{
				if (aiService is AzureOpenAIChatCompletion azureOpenAIChatCompletion)
				{
					services.SetService<IAIService>(azureOpenAIChatCompletion.ModelId, azureOpenAIChatCompletion, true);
				}
			}
		}

		List<ParameterView> askParameterView = new List<ParameterView>();
		// check if this.inputs has a variable with the name "ask"
		if (!this.inputs.Any(input => input.Name == "ask"))
		{
			askParameterView.Add(new ParameterView("ask", typeof(string), "The question to ask " + this.Name, IsRequired: true));
		}
		foreach (var input in this.inputs)
		{
			askParameterView.Add(new ParameterView(input.Name, typeof(string), input.Description, IsRequired: true));
		}

		List<ParameterView> replyBackParameterView = new List<ParameterView>
        {
            new ParameterView("reply", typeof(string), "The question to ask " + this.Name, IsRequired: true),
            new ParameterView("threadId", typeof(string), "The ID of the previous thread with " + this.Name, IsRequired: true)
        };
		foreach (var input in this.inputs)
		{
			replyBackParameterView.Add(new ParameterView(input.Name, typeof(string), input.Description, IsRequired: true));
		}

		// Create functions so other kernels can use this kernel as a plugin
		// TODO: make it possible for the ask function to have additional parameters based on the instruction template
		this.functions = new List<ISKFunction>
        {
            NativeFunction.FromNativeFunction(
                this.AskAsync,
                "Ask",
                "Use this function to ask "+this.Name+" a request.\nDescription of the " +this.Name+" assistant: " + this.Description+"\nYou may call this function in parallel to start multiple threads with the "+this.Name+" assistant.",
				askParameterView
            )
            // NativeFunction.FromNativeFunction(
            //     this.ReplyBackAsync,
            //     "ReplyBack",
            //     "If the response from "+this.Name+"-Ask requires a reply, use this function to reply back to the same thread",
			// 	replyBackParameterView
            // )
        };
	}

	private async Task<ModelRequest> GetInstructionsAsync(
		Dictionary<string, object?> variables = default
	)
	{
		string renderedTemplate = await kernel.PromptTemplateEngine.RenderAsync(this, this.instructions, variables);
		renderedTemplate = "<request>" + renderedTemplate + "</request>";
		return modelRequestXmlConverter.ParseXml(renderedTemplate, defaultRole: "system");
	}

	public async Task<FunctionResult> RunAsync(
		IThread thread,
		Dictionary<string, object?> variables = default,
		bool streaming = false,
		CancellationToken cancellationToken = default
	)
	{
		// Initialize the agent if it doesn't exist
		await InitializeAgentAsync();

		// Get the instructions for the assistant
		var renderedPrompt = await GetInstructionsAsync(variables);

		// Check if the first message in the instructions is a system message
		if (renderedPrompt.Messages[0].Role != "system")
		{
			throw new Exception("The first message in the instructions must be a system message");
		}
		string instructions = renderedPrompt.Messages[0].Content.ToString()!;
		
		// Check if there is a user message in the rendered prompt
		if (renderedPrompt.Messages.Count > 1 && renderedPrompt.Messages[1].Role == "user")
		{
			// Add the user message to the thread
			thread.AddUserMessageAsync(renderedPrompt.Messages[1].Content.ToString()!);
		}

		// Clone variables and add the instructions (check if variables is null first)
		var variablesWithInstructions = variables == null ? new Dictionary<string, object?>() : new Dictionary<string, object?>(variables);
		variablesWithInstructions.Add("instructions", instructions);

		// Invoke the thread
		return await thread.InvokeAsync(this, variablesWithInstructions, streaming, cancellationToken);
	}

	public List<FunctionView> GetFunctionViews()
	{
		List<FunctionView> functionViews = new List<FunctionView>();

		foreach (var plugin in this.plugins)
		{
			foreach (var function in plugin.Functions)
			{
				FunctionView initialFunctionView = function.Describe2();
				functionViews.Add(new FunctionView(
					initialFunctionView.Name,
					plugin.Name,
					initialFunctionView.Description,
					initialFunctionView.Parameters
				));
			}
		}

		return functionViews;
	}

	public async Task<IThread> CreateThreadAsync()
	{
		string url = "https://api.openai.com/v1/threads";
		using var httpRequestMessage = HttpRequest.CreatePostRequest(url);
		httpRequestMessage.Headers.Add("Authorization", $"Bearer {this.apiKey}");
		httpRequestMessage.Headers.Add("OpenAI-Beta", "assistants=v1");
		using var response = await this.client.SendAsync(httpRequestMessage).ConfigureAwait(false);
		string responseBody = await response.Content.ReadAsStringAsync();
		ThreadModel threadModel = JsonSerializer.Deserialize<ThreadModel>(responseBody);
		return new OpenAIThread(threadModel.Id, apiKey, this);
	}
	

	public ISKFunction RegisterCustomFunction(ISKFunction customFunction)
	{
		return this.kernel.RegisterCustomFunction(customFunction);
	}
	public SKContext CreateNewContext(ContextVariables? variables = null, IReadOnlyFunctionCollection? functions = null, ILoggerFactory? loggerFactory = null, CultureInfo? culture = null)
	{
		return this.kernel.CreateNewContext(variables, functions, loggerFactory, culture);
	}
	public T GetService<T>(string? name = null) where T : IAIService
	{
		return this.kernel.GetService<T>(name);
	}
	public IAIService GetDefaultService(string? name = null)
	{
		return this.AIServices[0];
	}
	public List<IAIService> GetAllServices()
	{
		return this.AIServices;
	}

	private async Task InitializeAgentAsync()
	{
		// Create new agent if it doesn't exist
		if (Id == null)
		{
			var requestData = new
			{
				model = ((AIService)this.AIServices[0]).ModelId
			};

			string url = "https://api.openai.com/v1/assistants";
        	using var httpRequestMessage = HttpRequest.CreatePostRequest(url, requestData);

			httpRequestMessage.Headers.Add("Authorization", $"Bearer {this.apiKey}");
			httpRequestMessage.Headers.Add("OpenAI-Beta", "assistants=v1");

       		using var response = await this.client.SendAsync(httpRequestMessage).ConfigureAwait(false);
			string responseBody = await response.Content.ReadAsStringAsync();
			AssistantModel assistantModel = JsonSerializer.Deserialize<AssistantModel>(responseBody)!;
			this.Id = assistantModel.Id;
		}
	}

	private async Task<IThread> GetThreadAsync(string threadId)
	{

		string url = "https://api.openai.com/v1/threads/"+threadId;
		using var httpRequestMessage = HttpRequest.CreateGetRequest(url);

		httpRequestMessage.Headers.Add("Authorization", $"Bearer {this.apiKey}");
		httpRequestMessage.Headers.Add("OpenAI-Beta", "assistants=v1");

		using var response = await this.client.SendAsync(httpRequestMessage).ConfigureAwait(false);
		string responseBody = await response.Content.ReadAsStringAsync();
		var threadModel = JsonSerializer.Deserialize<ThreadModel>(responseBody)!;
		return new OpenAIThread(threadModel.Id, apiKey, this);
	}

	private async Task<string> SendMessageAsync(string ask, string threadId, Dictionary<string, object> variables)
	{
		// Hack to show logging in terminal
		Console.ForegroundColor = ConsoleColor.Blue;
		Console.Write("ProjectManager");
		Console.ResetColor();
		Console.Write(" to ");
		Console.ForegroundColor = ConsoleColor.Green;
		Console.Write(this.Name);
		Console.ResetColor();
		Console.Write(" > ");
		Console.ForegroundColor = ConsoleColor.Blue;
		Console.WriteLine(ask);
		Console.ResetColor();

		// Create a new thread if one is not provided
		IThread thread;
		if (threadId == null)
		{
			// Create new thread
			thread = await CreateThreadAsync();
		}
		else
		{
			// Retrieve existing thread
			thread = await GetThreadAsync(threadId);
		}

		// Add the ask to the thread
		thread.AddUserMessageAsync(ask);

		var results = await this.RunAsync(
			thread,
			variables: variables
		);

		List<ModelMessage> modelMessages = results.GetValue<List<ModelMessage>>()!;

		// Concatenate all the messages from the model
		string resultsString = String.Join("\n",modelMessages.Select(modelMessage => modelMessage.ToString()));

		// Hack to show logging in terminal
		Console.ForegroundColor = ConsoleColor.Green;
		Console.Write(this.Name);
		Console.ResetColor();
		Console.Write(" to ");
		Console.ForegroundColor = ConsoleColor.Blue;
		Console.Write("ProjectManager");
		Console.ResetColor();
		Console.Write(" > ");
		Console.ForegroundColor = ConsoleColor.Green;
		Console.WriteLine(resultsString);
		Console.ResetColor();

		// TODO: return AskResponse object once kernel supports complex types
		// return new AskResponse() {
		// 	ThreadId = thread.Id,
		// 	Response = resultsString
		// };

		return JsonSerializer.Serialize(new AskResponse() {
			ThreadId = thread.Id,
			Response = resultsString,
			Instructions = "Use the "+this.Name+"-ReplyBack function if you need to continue the conversation on this thread"
		});
	}

	// This is the function that is provided as part of the IPlugin interface
	private async Task<string> AskAsync(Dictionary<string, object> variables)
	{
		IThread thread = await CreateThreadAsync();

		return await SendMessageAsync(variables["ask"].ToString(), thread.Id, variables);
	}

	private async Task<string> ReplyBackAsync(Dictionary<string, object> variables)
	{
		return await this.SendMessageAsync(variables["ask"].ToString(), variables["threadId"].ToString(), variables);
	}


	public IPromptTemplateEngine PromptTemplateEngine => this.kernel.PromptTemplateEngine;

	public IReadOnlyFunctionCollection Functions => this.kernel.Functions;

	public IDelegatingHandlerFactory HttpHandlerFactory => this.kernel.HttpHandlerFactory;

	public ILoggerFactory LoggerFactory => throw new NotImplementedException();

	public ISemanticTextMemory Memory => throw new NotImplementedException();

	public IReadOnlyFunctionCollection Skills => throw new NotImplementedException();

    public event EventHandler<FunctionInvokingEventArgs>? FunctionInvoking;
	public event EventHandler<FunctionInvokedEventArgs>? FunctionInvoked;


	public ISKFunction Func(string pluginName, string functionName)
	{
		throw new NotImplementedException();
	}

	public IDictionary<string, ISKFunction> ImportSkill(object functionsInstance, string? pluginName = null)
	{
		throw new NotImplementedException();
	}

	public void RegisterMemory(ISemanticTextMemory memory)
	{
		throw new NotImplementedException();
	}

	public Task<KernelResult> RunAsync(ContextVariables variables, CancellationToken cancellationToken, params ISKFunction[] pipeline)
	{
		throw new NotImplementedException();
	}
}