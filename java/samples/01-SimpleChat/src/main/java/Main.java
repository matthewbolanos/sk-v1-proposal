import java.nio.file.Path;

import com.azure.ai.openai.OpenAIAsyncClient;
import com.azure.ai.openai.OpenAIClientBuilder;
import com.azure.core.credential.KeyCredential;
import com.microsoft.semantickernel.Kernel;
import com.microsoft.semantickernel.KernelResult;
import com.microsoft.semantickernel.SKBuilders;
import com.microsoft.semantickernel.chatcompletion.ChatCompletion;
import com.microsoft.semantickernel.chatcompletion.ChatHistory;
import com.microsoft.semantickernel.exceptions.ConfigurationException;
import com.microsoft.semantickernel.orchestration.ContextVariables;
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
        SKFunction chatFunction = SemanticFunction.fromYaml(yamlPath);

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

        ChatHistory chatHistory = gpt35Turbo.createNewChat();
        while(true)
        {
            String input = System.console().readLine("User > ");
            chatHistory.addUserMessage(input);

            // Run the simple chat
            // The simple chat function uses the messages variable to generate the next message
            // see Plugins/ChatPlugin/SimpleChat.prompt.yaml for the full prompt
            KernelResult result = kernel.runAsync(
                true, // streaming
                ContextVariables.builder().withVariable("messages", chatHistory).build(),
                chatFunction
            ).block();

            System.console().printf("Assistant > ");
            result.functionResults().forEach(
                functionResult -> {
                    functionResult.<String>getStreamingValueAsync().subscribe(
                        message -> System.console().printf(message)
                    ); 
                    String message = functionResult.<String>getValueAsync().block();
                    chatHistory.addAssistantMessage(message);
                }
            );
        }
    }
}