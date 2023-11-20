
import java.nio.file.Path;

import com.azure.ai.openai.OpenAIAsyncClient;
import com.azure.ai.openai.OpenAIClientBuilder;
import com.azure.core.credential.KeyCredential;
import com.microsoft.semantickernel.Kernel;
import com.microsoft.semantickernel.SKBuilders;
import com.microsoft.semantickernel.chatcompletion.ChatCompletion;
import com.microsoft.semantickernel.chatcompletion.ChatHistory;
import com.microsoft.semantickernel.exceptions.ConfigurationException;
import com.microsoft.semantickernel.orchestration.SKFunction;
import com.microsoft.semantickernel.v1.semanticfunctions.SemanticFunction;
import com.microsoft.semantickernel.v1.templateengine.HandlebarsPromptTemplateEngine;

public class Main {
    
    final static String GPT_35_DEPLOYMENT_NAME = System.getenv("GPT_35_DEPLOYMENT_NAME");
    final static String GPT_4_DEPLOYMENT_NAME = System.getenv("GPT_4_DEPLOYMENT_NAME");
    final static String AZURE_OPENAI_ENDPOINT = System.getenv("AZURE_OPENAI_ENDPOINT");
    final static String AZURE_OPENAI_API_KEY = System.getenv("AZURE_OPENAI_API_KEY");
    final static String CURRENT_DIRECTORY = System.getProperty("user.dir");
    
    
    public static void main(String[] args) throws ConfigurationException {

        OpenAIAsyncClient client = new OpenAIClientBuilder()
            .credential(new KeyCredential(AZURE_OPENAI_API_KEY))
            .endpoint(AZURE_OPENAI_ENDPOINT)
            .buildAsyncClient();


        // Initialize the required functions and services for the kernel
        Path yamlPath = Path.of(CURRENT_DIRECTORY + "/Plugins/ChatPlugin/PersonaChat.prompt.yaml");
        SKFunction<?> chatFunction = SemanticFunction.fromYaml(yamlPath);

        ChatCompletion<ChatHistory> gpt35Turbo = ChatCompletion.builder()
            .withOpenAIClient(client)
            .withModelId(GPT_35_DEPLOYMENT_NAME)
            .build();

        ChatCompletion<ChatHistory> gpt4 = ChatCompletion.builder()
                .withOpenAIClient(client)
                .withModelId(GPT_4_DEPLOYMENT_NAME)
                .build();


        Kernel kernel = SKBuilders.kernel()
            .withDefaultAIService(gpt35Turbo)
            .withDefaultAIService(gpt4)
            .withPromptTemplateEngine(new HandlebarsPromptTemplateEngine())
            .build();
    }
}
/*
string Gpt35TurboDeploymentName = Env.Var("AzureOpenAI:Gpt35TurboDeploymentName")!;
string Gpt4DeploymentName = Env.Var("AzureOpenAI:Gpt4DeploymentName")!;
string AzureOpenAIEndpoint = Env.Var("AzureOpenAI:Endpoint")!;
string AzureOpenAIApiKey = Env.Var("AzureOpenAI:ApiKey")!;
string currentDirectory = Directory.GetCurrentDirectory();

// Initialize the required functions and services for the kernel
ISKFunction chatFunction = SemanticFunction.GetFunctionFromYaml(currentDirectory + "/Plugins/ChatPlugin/PersonaChat.prompt.yaml");
IChatCompletion gpt35Turbo = new AzureOpenAIChatCompletion("gpt-3.5-turbo", AzureOpenAIEndpoint, AzureOpenAIApiKey, Gpt35TurboDeploymentName);
IChatCompletion gpt4 = new AzureOpenAIChatCompletion("gpt-4", AzureOpenAIEndpoint, AzureOpenAIApiKey, Gpt4DeploymentName);

// Create new kernel
IKernel kernel = new Kernel(
aiServices: new () { gpt35Turbo, gpt4 },
promptTemplateEngines: new () {new HandlebarsPromptTemplateEngine()}
);

// Start the chat
ChatHistory chatHistory = gpt35Turbo.CreateNewChat();
while(true)
{
    Console.Write("User > ");
    chatHistory.AddUserMessage(Console.ReadLine()!);
    
    // Run the chat function
    // The persona chat function uses the persona variable to set the persona of the chat using a system message
    // See Plugins/ChatPlugin/PersonaChat.prompt.yaml for the full prompt
    var result = await kernel.RunAsync(
    chatFunction,
    variables: new() {
        { "persona", "You are a snarky (yet helpful) teenage assistant. Make sure to use hip slang in every response." },
        { "messages", chatHistory }
    },
    streaming: true
    );
    
    Console.Write("Assistant > ");
    await foreach(var message in result.GetStreamingValue<string>()!)
    {
        Console.Write(message);
    }
    Console.WriteLine();
    chatHistory.AddAssistantMessage(await result.GetValueAsync<string>()!);
}
*/