package com.microsoft.semantickernel.v1.plugin;

import com.microsoft.semantickernel.orchestration.SKFunction;
import java.util.Collection;

public class Plugin implements com.microsoft.semantickernel.plugin.Plugin {
    
    public Plugin(String name, String description, SKFunction... functions) {
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
