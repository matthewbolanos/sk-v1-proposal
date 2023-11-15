package com.microsoft.semantickernel.v1;

import com.azure.ai.openai.OpenAIAsyncClient;
import com.microsoft.semantickernel.Kernel;
import com.microsoft.semantickernel.SKBuilders;
import com.microsoft.semantickernel.connectors.ai.openai.util.OpenAIClientProvider;
import com.microsoft.semantickernel.exceptions.ConfigurationException;

public class Main {
    public static void main(String[] args) throws ConfigurationException {
        OpenAIAsyncClient client = OpenAIClientProvider.getClient();

        Kernel kernel = SKBuilders.kernel()
                .withDefaultAIService(SKBuilders.textCompletion()
                        .withOpenAIClient(client)
                        .withModelId("text-davinci-003")
                        .build())
                .build();

    }
}