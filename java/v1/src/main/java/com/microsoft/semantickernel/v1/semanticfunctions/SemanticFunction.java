package com.microsoft.semantickernel.v1.semanticfunctions;

import java.nio.file.Path;

import com.microsoft.semantickernel.orchestration.SKFunction;

public interface SemanticFunction
{

    public static SKFunction getFunctionFromYaml(Path filePath)
    {
        return null;
    }

    public static SKFunction getFunctionFromYaml(String yamlContent)
    {
        return null;
    }
}