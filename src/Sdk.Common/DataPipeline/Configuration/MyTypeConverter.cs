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
        // Stellen Sie sicher, dass der Parser auf die richtige Position gesetzt ist
        parser.MoveNext();

        // Annahme: Das erste Element im YAML ist ein Mapping
        if (parser.Current is Scalar key)
        {
            if (key.Value != "type")
            {
                throw DataPipelineException.FirstElementMustBeType(parser.Current.Start);
            }
        }
        
        parser.MoveNext();

        // Extrahieren Sie das Schlüssel-Wert-Paar
        // var key = parser.Current?.Value;
        // parser.MoveNext();
        // var value = parser.Current?.Value;
        //
        // // Bewegen Sie den Parser über den Wert hinaus
        // parser.MoveNext();
        //
        // // Überprüfen Sie, ob der Typ im Mapping vorhanden ist
        // if (_typeMapping.TryGetValue(value, out var mappedType))
        // {
        //     var deserializer = new DeserializerBuilder().Build();
        //     parser = new MergingParser(parser);
        //     return deserializer.Deserialize(parser, mappedType);
        // }

        throw new InvalidOperationException("Unbekannter Typ im YAML-Dokument.");
    }

    /// <inheritdoc />
    public void WriteYaml(IEmitter emitter, object? value, Type type)
    {
        // Implementieren Sie die Serialisierungslogik, falls erforderlich
    }
}