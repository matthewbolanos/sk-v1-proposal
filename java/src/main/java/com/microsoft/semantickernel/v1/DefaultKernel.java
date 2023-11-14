package com.microsoft.semantickernel.v1;

import com.microsoft.semantickernel.KernelConfig;
import com.microsoft.semantickernel.memory.SemanticTextMemory;
import com.microsoft.semantickernel.services.AIServiceProvider;
import com.microsoft.semantickernel.templateengine.PromptTemplateEngine;

import javax.annotation.Nullable;

public class DefaultKernel extends com.microsoft.semantickernel.DefaultKernel {
    public DefaultKernel(KernelConfig kernelConfig, PromptTemplateEngine promptTemplateEngine, @Nullable SemanticTextMemory memoryStore, AIServiceProvider aiServiceProvider) {
        super(kernelConfig, promptTemplateEngine, memoryStore, aiServiceProvider);
    }
}
