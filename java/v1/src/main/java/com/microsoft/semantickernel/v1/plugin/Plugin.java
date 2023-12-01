package com.microsoft.semantickernel.v1.plugin;

import java.util.Collection;

import com.microsoft.semantickernel.orchestration.SKFunction;

public class Plugin implements com.microsoft.semantickernel.plugin.Plugin {
    
    public Plugin(String name, SKFunction... functions) {
    }

    public Plugin(String name, Collection<SKFunction> functions) {
    }

    public String name() {
        return null;
    }

    public String description() {
        return null;
    }
    
    public Collection<SKFunction> functions() {
        return null;
    }
}
