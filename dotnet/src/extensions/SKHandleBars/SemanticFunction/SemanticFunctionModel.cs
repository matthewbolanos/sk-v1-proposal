// Copyright (c) Microsoft. All rights reserved.
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Microsoft.SemanticKernel.Handlebars;

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

internal sealed class SemanticFunctionModel
{
    [YamlMember(Alias = "name")]
    public string Name { get; set; }

    [YamlMember(Alias = "template")]
    public string Template { get; set; }

    [YamlMember(Alias = "template_format")]
    public string TemplateFormat { get; set; }

    [YamlMember(Alias = "description")]
    public string Description { get; set; }


    [YamlMember(Alias = "input_variables")]
    public List<VariableViewModel> InputVariables { get; set; }

    [YamlMember(Alias = "output_variable")]
    public VariableViewModel OutputVariable { get; set; }

    [YamlMember(Alias = "execution_settings")]
    public List<ExecutionSettingsModel> ExecutionSettings { get; set; }
}

public class VariableViewModel
{
    [YamlMember(Alias = "name")]
    public string Name { get; set;   }

    [YamlMember(Alias = "type")]
    public string Type { get; set;  }

    [YamlMember(Alias = "description")]
    public string Description { get; set;  }

    [YamlMember(Alias = "default_value")]
    public dynamic DefaultValue { get; set;   }

    [YamlMember(Alias = "is_required")]
    public bool IsRequired { get; set;  }
}

public class ExecutionSettingsModel
{
    [YamlMember(Alias = "model_id")]
    public string ModelId { get; set; }

    [YamlMember(Alias = "model_id_pattern")]
    public string ModelIdPattern { get; set; }


    [YamlMember(Alias = "service_id")]
    public string ServiceId { get; set; }

    // Dictionary to store arbitrary additional properties
    private readonly Dictionary<string, object> _additionalProperties = new Dictionary<string, object>();

    [YamlIgnore] // We don't want the YAML serializer to touch this directly
    public object this[string propertyName]
    {
        get
        {
            _additionalProperties.TryGetValue(propertyName, out var value);
            return value;
        }
        set
        {
            _additionalProperties[propertyName] = value;
        }
    }
}

public class ExecutionSettingsModelConverter : IYamlTypeConverter
{
    public bool Accepts(Type type)
    {
        return type == typeof(ExecutionSettingsModel);
    }

    public object? ReadYaml(IParser parser, Type type)
    {
        var model = new ExecutionSettingsModel();

        parser.Expect<MappingStart>();
        while (!parser.TryConsume<MappingEnd>(out _))
        {
            var key = parser.Consume<Scalar>().Value;

            switch (key)
            {
                case "model_id":
                    model.ModelId = parser.Consume<Scalar>().Value;
                    break;
                case "model_id_pattern":
                    model.ModelIdPattern = parser.Consume<Scalar>().Value;
                    break;
                case "service_id":
                    model.ServiceId = parser.Consume<Scalar>().Value;
                    break;
                default:
                    model[key] = parser.Consume<Scalar>().Value;
                    break;
            }
        }

        return model;
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type)
    {
        throw new NotImplementedException();
    }
}