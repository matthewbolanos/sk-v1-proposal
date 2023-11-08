

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

public class AssistantKernel : IKernel, IPlugin
{
	private readonly Microsoft.SemanticKernel.Kernel kernel;

	private readonly List<IPlugin> plugins;
	private readonly List<IAIService> AIServices;

	public string Id { get; private set; }
	public string Name { get; }

    public string? Description { get; }

    public string? Instructions { get; }

    IEnumerable<ISKFunction> IPlugin.Functions {
		get { return this.functions; }
	}

	private HttpClient client = new HttpClient();

	private readonly string apiKey;

	private readonly string model;

	private readonly List<ISKFunction> functions;

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

		return new AssistantKernel(
			assistantKernelModel.Name,
			assistantKernelModel.Description,
			assistantKernelModel.Template,
			aiServices,
			plugins,
			promptTemplateEngines
		);
	}

	public AssistantKernel(
		string name,
		string? description,
		string? instructions,
		List<IAIService>? aiServices = null,
		List<IPlugin>? plugins = null,
		List<IPromptTemplateEngine>? promptTemplateEngines = null
	)
	{
		this.Name = name;
		this.Description = description;
		this.Instructions = instructions;
		this.AIServices = aiServices;
		
		// Grab the first AI service for the apiKey and model for the Assistants API
		this.apiKey = ((OpenAIChatCompletion)this.AIServices[0]).ApiKey;
		this.model = ((OpenAIChatCompletion)this.AIServices[0]).ModelId;
		
		// Create a function collection using the plugins
		FunctionCollection functionCollection = new FunctionCollection();
		this.plugins = plugins ?? new List<IPlugin>();
		if (plugins != null)
		{
			foreach(IPlugin plugin in plugins)
			{
				foreach(ISKFunction function in plugin.Functions)
				{
					functionCollection.AddFunction(plugin.Name, function);
				}
			}
		}

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
		
		// Initialize the prompt template engine
		IPromptTemplateEngine promptTemplateEngine;
		if (promptTemplateEngines != null && promptTemplateEngines.Count > 0)
		{
			promptTemplateEngine = promptTemplateEngines[0];
		}
		else
		{
			promptTemplateEngine = new HandlebarsPromptTemplateEngine();
		}

		// Create underlying kernel
		this.kernel = new SemanticKernel.Kernel(
			functionCollection,
			services.Build(),
			promptTemplateEngine,
			null!,
			NullHttpHandlerFactory.Instance,
			null
		);

		// Create functions so other kernels can use this kernel as a plugin
		this.functions = new List<ISKFunction>
        {
            NativeFunction.FromNativeFunction(
                this.AskAsync,
                "Ask",
                this.Description
            )
        };
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

		// Invoke the thread
		return await thread.InvokeAsync(this, variables, streaming, cancellationToken);
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
		
		var requestData = new
		{
			thread_id = threadId
		};

		string url = "https://api.openai.com/v1/threads";
		using var httpRequestMessage = HttpRequest.CreateGetRequest(url, requestData);

		httpRequestMessage.Headers.Add("Authorization", $"Bearer {this.apiKey}");
		httpRequestMessage.Headers.Add("OpenAI-Beta", "assistants=v1");

		using var response = await this.client.SendAsync(httpRequestMessage).ConfigureAwait(false);
		string responseBody = await response.Content.ReadAsStringAsync();
		var threadModel = JsonSerializer.Deserialize<ThreadModel>(responseBody)!;
		return new OpenAIThread(threadModel.Id, apiKey, this);
	}

	private async Task<string> AskAsync(string ask, string? threadId = default)
	{
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

		var results = await this.RunAsync(
			thread,
			variables: new Dictionary<string, object?>()
			{
				{ "ask", ask }
			}
		);

		return results.GetValue<string>()!;
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