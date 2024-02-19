using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Meshmakers.Octo.Sdk.Common.DataPipeline.Configuration;

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
    public YamlPipelineConfigurationSerializer()
    {
        _serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithEmissionPhaseObjectGraphVisitor(args => new CustomYamlTypeAttributeAppender(args.InnerVisitor))
            .Build();
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithTypeConverter(new MyTypeConverter())
         //   .WithNodeTypeResolver(new ConfigurationNodeResolver())
            .Build();
    }
    
    /// <inheritdoc />
    public Task SerializeAsync(StreamWriter streamWriter, PipelineConfigurationRoot pipelineConfiguration)
    {
        _serializer.Serialize(streamWriter, pipelineConfiguration);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<PipelineConfigurationRoot> DeserializeAsync(Stream stream, CancellationToken? cancellationToken = null)
    {
        using var streamReader = new StreamReader(stream);
        var configurationRoot = _deserializer.Deserialize<PipelineConfigurationRoot>(streamReader);
        return Task.FromResult(configurationRoot);
    }
}