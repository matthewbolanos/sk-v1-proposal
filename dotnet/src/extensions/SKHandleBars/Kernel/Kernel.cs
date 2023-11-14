

using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextEmbedding;
using Microsoft.SemanticKernel.Events;
using Microsoft.SemanticKernel.Http;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Plugins.Memory;
using Microsoft.SemanticKernel.Services;
using Microsoft.SemanticKernel.TemplateEngine;
using Microsoft.SemanticKernel.TemplateEngine.Basic;

namespace Microsoft.SemanticKernel.Handlebars;

public class Kernel : IKernel
{
	protected readonly Microsoft.SemanticKernel.Kernel kernel;
	public ISKFunction? EntryPoint { get; }

	protected readonly List<IPlugin> plugins;
	protected readonly List<IAIService> AIServices;

	public Kernel(
		List<IAIService>? aiServices = null,
		List<IPlugin>? plugins = null,
		List<IPromptTemplateEngine>? promptTemplateEngines = null,
		ISKFunction? entryPoint = null
	)
	{
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

		this.EntryPoint = entryPoint;
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