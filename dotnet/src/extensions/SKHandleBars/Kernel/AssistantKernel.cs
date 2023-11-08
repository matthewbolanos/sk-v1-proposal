

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

    IEnumerable<ISKFunction> IPlugin.Functions => this.plugins.SelectMany(plugin => plugin.Functions);

	private HttpClient client = new HttpClient();

	private readonly string apiKey;

	public static AssistantKernel FromConfiguration(
		string configurationFile,
		string apiKey,
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

		// Check if there are any promptTemplateEngines provided
		if (promptTemplateEngines == null)
		{
			promptTemplateEngines = new List<IPromptTemplateEngine>(){
				new HandlebarsPromptTemplateEngine()
			};
		}

		return new AssistantKernel(
			assistantKernelModel.Name,
			assistantKernelModel.Description,
			assistantKernelModel.Template,
			apiKey,
			aiServices,
			plugins,
			promptTemplateEngines,
			promptTemplateEngines[0]
		);
	}

	public AssistantKernel(
		string name,
		string? description,
		string? instructions,
		string apiKey,
		List<IAIService>? aiServices = null,
		List<IPlugin>? plugins = null,
		List<IPromptTemplateEngine>? promptTemplateEngines = null,
		IPromptTemplateEngine? instructionsTemplateEngine = null
	)
	{
		this.Name = name;
		this.Description = description;
		this.Instructions = instructions;
		this.apiKey = apiKey;

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
		Dictionary<Type, string> defaultIds = new(){
			{ typeof(IAIService), "gpt-35-turbo" }
		};

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

		IPromptTemplateEngine promptTemplateEngine;
		if (promptTemplateEngines != null && promptTemplateEngines.Count > 0)
		{
			promptTemplateEngine = promptTemplateEngines[0];
		}
		else
		{
			promptTemplateEngine = new HandlebarsPromptTemplateEngine();
		}

		this.AIServices = aiServices;

		this.kernel = new SemanticKernel.Kernel(
			functionCollection,
			services.Build(),
			promptTemplateEngine,
			null!,
			NullHttpHandlerFactory.Instance,
			null
		);
	}

	public async Task<FunctionResult> RunAsync(IThread thread)
	{
		await InitializeAgent();

		var requestData = new
		{
			assistant_id = this.Id,
			instructions = this.Instructions
		};

		string url = "https://api.openai.com/v1/threads/"+thread.Id+"/runs";
        using var httpRequestMessage = HttpRequest.CreatePostRequest(url, requestData);

        httpRequestMessage.Headers.Add("Authorization", $"Bearer {this.apiKey}");
        httpRequestMessage.Headers.Add("OpenAI-Beta", "assistants=v1");

        var response = await this.client.SendAsync(httpRequestMessage).ConfigureAwait(false);

		string responseBody = await response.Content.ReadAsStringAsync();
		ThreadRunModel threadRunModel = JsonSerializer.Deserialize<ThreadRunModel>(responseBody);

		// poll the run until it is complete
		while (threadRunModel.Status == "queued" || threadRunModel.Status == "in_progress")
		{
			// add a delay
			await Task.Delay(300);

			url = "https://api.openai.com/v1/threads/"+thread.Id+"/runs/"+threadRunModel.Id;
			using var httpRequestMessage2 = HttpRequest.CreateGetRequest(url);

			httpRequestMessage2.Headers.Add("Authorization", $"Bearer {this.apiKey}");
			httpRequestMessage2.Headers.Add("OpenAI-Beta", "assistants=v1");

			response = await this.client.SendAsync(httpRequestMessage2).ConfigureAwait(false);

			responseBody = await response.Content.ReadAsStringAsync();
			try {
				threadRunModel = JsonSerializer.Deserialize<ThreadRunModel>(responseBody);
			} catch (Exception e) {
				Console.WriteLine(responseBody);
				throw e;
			}
		}

		if (threadRunModel.Status == "failed")
		{
			return new FunctionResult(this.Name, "Ask", new List<ModelMessage>(){
				{ new ModelMessage(threadRunModel.LastError.Message) }
			});
		}

		// get the steps
		ThreadRunStepListModel threadRunSteps = await GetThreadRunSteps(thread.Id, threadRunModel.Id);

		// check step details
		List<ModelMessage> messages = new List<ModelMessage>();
		foreach(ThreadRunStepModel threadRunStep in threadRunSteps.Data)
		{
			if (threadRunStep.StepDetails.Type == "message_creation")
			{
				// Get message Id
				var messageId = threadRunStep.StepDetails.MessageCreation.MessageId;
				ModelMessage message = await thread.RetrieveMessageAsync(messageId);
				messages.Add(message);
			}
		}
				
		return new FunctionResult(this.Name, "Ask", messages);
	}

	private async Task<ThreadRunStepListModel> GetThreadRunSteps(string threadId, string runId)
	{
		string url = "https://api.openai.com/v1/threads/"+threadId+"/runs/"+runId+"/steps";
        using var httpRequestMessage = HttpRequest.CreateGetRequest(url);

        httpRequestMessage.Headers.Add("Authorization", $"Bearer {this.apiKey}");
        httpRequestMessage.Headers.Add("OpenAI-Beta", "assistants=v1");

        var response = await this.client.SendAsync(httpRequestMessage).ConfigureAwait(false);

		string responseBody = await response.Content.ReadAsStringAsync();
		return JsonSerializer.Deserialize<ThreadRunStepListModel>(responseBody);
	}

	private async Task InitializeAgent()
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
			AssistantModel assistantModel = JsonSerializer.Deserialize<AssistantModel>(responseBody);
			this.Id = assistantModel.Id;
		}
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

	public async Task<IThread> CreateThread()
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