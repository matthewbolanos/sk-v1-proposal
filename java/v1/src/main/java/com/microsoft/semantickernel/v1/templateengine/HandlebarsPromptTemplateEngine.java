package com.microsoft.semantickernel.v1.templateengine;

import java.util.List;

import com.microsoft.semantickernel.orchestration.SKContext;
import com.microsoft.semantickernel.templateengine.PromptTemplateEngine;
import com.microsoft.semantickernel.templateengine.blocks.Block;

import reactor.core.publisher.Mono;

public class HandlebarsPromptTemplateEngine implements PromptTemplateEngine {

    @Override
    public Mono<String> renderAsync(String s, SKContext skContext) {
        return null;
    }

    @Override
    public List<Block> extractBlocks(String s) {
        return null;
    }
}
