package com.microsoft.semantickernel;

import com.azure.ai.openai.OpenAIAsyncClient;
import com.microsoft.semantickernel.chatcompletion.ChatCompletion;
import com.microsoft.semantickernel.connectors.ai.openai.util.OpenAIClientProvider;
import com.microsoft.semantickernel.exceptions.ConfigurationException;

public class Main {
    public static void main(String[] args) throws ConfigurationException {
        OpenAIAsyncClient client = OpenAIClientProvider.getClient();

        Kernel kernel = SKBuilders.kernel()
                .withDefaultAIService(SKBuilders.chatCompletion()
                        .withOpenAIClient(client)
                        .withModelId("gpt3-turbo")
                        .build())
                .build();
    }
}