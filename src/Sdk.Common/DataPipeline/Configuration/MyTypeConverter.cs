using Meshmakers.Octo.Sdk.Common.DataPipeline.Nodes.Objects;
using Meshmakers.Octo.Sdk.Common.DataPipeline.Nodes.Signals;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Meshmakers.Octo.Sdk.Common.DataPipeline.Configuration;

/// <summary>
/// 
/// </summary>
public class MyTypeConverter : IYamlTypeConverter
{
    private readonly Dictionary<string, Type> _typeMapping = new()
    {
        { "type1", typeof(AssignObjectConfigurationNode) },
        { "type2", typeof(LinearScalerConfigurationNode) },
    };
    
    /// <inheritdoc />
    public bool Accepts(Type type)
    {
        return typeof(ConfigurationNode).IsAssignableFrom(type);
    }

    /// <inheritdoc />
    public object? ReadYaml(IParser parser, Type type)
    {
        parser.MoveNext();

        if (parser.Current is Scalar key)
        {
            if (key.Value != "type")
            {
                throw DataPipelineException.FirstElementMustBeType(parser.Current.Start);
            }
        }
        
        parser.MoveNext();

        if (parser.Current is Scalar value)
        {
            if (_typeMapping.TryGetValue(value.Value, out var mappedType))
            {
                var deserializer = new DeserializerBuilder().Build();
                parser = new MergingParser(parser);
                return deserializer.Deserialize(parser, mappedType);
            }
        }
        

        throw new InvalidOperationException("Unbekannter Typ im YAML-Dokument.");
    }

    /// <inheritdoc />
    public void WriteYaml(IEmitter emitter, object? value, Type type)
    {
        // Implementieren Sie die Serialisierungslogik, falls erforderlich
    }
}