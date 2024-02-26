using System.Text.Json;
using System.Text.Json.Serialization;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration.Serializer;

/// <summary>
/// Implements the pipeline configuration serializer using JSON format
/// </summary>
public class JsonPipelineConfigurationSerializer : IJsonPipelineConfigurationSerializer
{
    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// Constructor
    /// </summary>
    public JsonPipelineConfigurationSerializer(INodeLookupService nodeLookupService)
    {
        _options = new JsonSerializerOptions
        {
            Converters =
            {
                new NodeConfigurationConverter<NodeConfiguration>(nodeLookupService)
            },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
    }

    /// <inheritdoc />
    public Task<string> SerializeAsync(PipelineConfigurationRoot pipelineConfiguration)
    {
        return Task.FromResult(JsonSerializer.Serialize(pipelineConfiguration, _options));
    }

    /// <inheritdoc />
    public async Task SerializeAsync(StreamWriter streamWriter, PipelineConfigurationRoot pipelineConfiguration)
    {
        await JsonSerializer.SerializeAsync(streamWriter.BaseStream, pipelineConfiguration, _options).ConfigureAwait(false);
    }
    
    /// <inheritdoc />
    public Task<PipelineConfigurationRoot> DeserializeAsync(string formattedText)
    {
        var pipelineConfigurationRoot = JsonSerializer.Deserialize<PipelineConfigurationRoot>(formattedText, _options);
        return Task.FromResult(pipelineConfigurationRoot ?? throw new Exception("Deserialization failed"));
    }

    /// <inheritdoc />
    public Task<PipelineConfigurationRoot> DeserializeAsync(Stream stream, CancellationToken? cancellationToken = null)
    {
        var pipelineConfigurationRoot = JsonSerializer.Deserialize<PipelineConfigurationRoot>(stream, _options);
        return Task.FromResult(pipelineConfigurationRoot ?? throw new Exception("Deserialization failed"));
    }
}