using System.Text.Json;
using System.Text.Json.Serialization;
using Meshmakers.Common.Shared;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration.Serializer;

internal class NodeConfigurationConverter<T> : JsonConverter<T> where T: NodeConfiguration
{
    private readonly INodeLookupService _nodeLookupService;

    public NodeConfigurationConverter(INodeLookupService nodeLookupService)
    {
        _nodeLookupService = nodeLookupService;
    }

    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
        {
            if (doc.RootElement.TryGetProperty(YamlFields.Type, out var typeDiscriminator))
            {
                var qualifiedName = typeDiscriminator.GetString();
                if (!string.IsNullOrWhiteSpace(qualifiedName))
                {
                    if (_nodeLookupService.TryGetConfigurationNodeType(qualifiedName, out var nodeType))
                    {
                        var x = JsonSerializer.Deserialize(doc.RootElement.GetRawText(), nodeType, options);
                        return (T?)x;
                    }
                    throw DataPipelineException.UnknownDiscriminator(qualifiedName);
                }
            }

            throw DataPipelineException.NoDiscriminatorFound();
        }
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        if (_nodeLookupService.TryGetNodeQualifiedName(value.GetType(), out var nodeQualifiedName))
        {
            writer.WriteString(YamlFields.Type, nodeQualifiedName);

            foreach (var prop in value.GetType().GetProperties())
            {
                if (prop.GetValue(value) != null)
                {
                    writer.WritePropertyName(prop.Name.ToCamelCase());
                    JsonSerializer.Serialize(writer, prop.GetValue(value), prop.PropertyType, options);
                }
            }

            writer.WriteEndObject();
            return;
        }

        throw DataPipelineException.UnknownConfigurationType(value.GetType());
    }
}
