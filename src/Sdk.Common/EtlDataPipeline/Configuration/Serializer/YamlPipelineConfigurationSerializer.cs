using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration.Serializer;

/// <summary>
/// Implements the pipeline configuration serializer using YAML
/// </summary>
public class YamlPipelineConfigurationSerializer : IPipelineConfigurationSerializer
{
    private readonly IDeserializer _deserializer;
    private readonly ISerializer _serializer;

    /// <summary>
    /// Constructor
    /// </summary>
    public YamlPipelineConfigurationSerializer(INodeLookupService nodeLookupService)
    {
        _serializer = new SerializerBuilder()
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults | DefaultValuesHandling.OmitEmptyCollections | DefaultValuesHandling.OmitNull)
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithEmissionPhaseObjectGraphVisitor(args => new NodeConfigurationTypeAppender(args.InnerVisitor, nodeLookupService))
            .Build();

        // The deserializer is configured to use the ConfigurationNodeTypeInspector and the ConfigurationNodeTypeDiscriminator
        // to ensure that the correct type is used for deserialization -> The order is important here
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithTypeInspector(innerTypeInspector => new NodeConfigurationTypeInspector(innerTypeInspector))
            .WithTypeDiscriminatingNodeDeserializer(o =>
            {
                o.AddTypeDiscriminator(new NodeConfigurationTypeDiscriminator(nodeLookupService));
            })
            .Build();
    }

    /// <inheritdoc />
    public Task<string> SerializeAsync(PipelineConfigurationRoot pipelineConfiguration)
    {
        return Task.FromResult(_serializer.Serialize(pipelineConfiguration));
    }

    /// <inheritdoc />
    public Task SerializeAsync(StreamWriter streamWriter, PipelineConfigurationRoot pipelineConfiguration)
    {
        _serializer.Serialize(streamWriter, pipelineConfiguration);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<PipelineConfigurationRoot> DeserializeAsync(string formattedText)
    {
        var configurationRoot = _deserializer.Deserialize<PipelineConfigurationRoot>(formattedText);
        return Task.FromResult(configurationRoot);
    }

    /// <inheritdoc />
    public Task<PipelineConfigurationRoot> DeserializeAsync(Stream stream, CancellationToken? cancellationToken = null)
    {
        using var streamReader = new StreamReader(stream);
        var configurationRoot = _deserializer.Deserialize<PipelineConfigurationRoot>(streamReader);
        return Task.FromResult(configurationRoot);
    }
}