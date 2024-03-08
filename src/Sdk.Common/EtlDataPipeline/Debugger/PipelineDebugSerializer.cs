using Newtonsoft.Json;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Debugger;

/// <summary>
/// Represents the serializer for debug information in the pipeline
/// </summary>
public class PipelineDebugSerializer : IPipelineDebugSerializer
{
    private readonly JsonSerializerSettings _serializerSettings;

    /// <summary>
    /// Constructor
    /// </summary>
    public PipelineDebugSerializer()
    {
        _serializerSettings = new JsonSerializerSettings
        {
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            NullValueHandling = NullValueHandling.Ignore
        };
    }

    /// <inheritdoc />
    public Task<string> SerializeAsync(DebugInformationRoot debugInformationRoot)
    {
        return Task.FromResult(JsonConvert.SerializeObject(debugInformationRoot, _serializerSettings));
    }

    /// <inheritdoc />
    public Task SerializeAsync(StreamWriter streamWriter, DebugInformationRoot debugInformationRoot)
    {
        JsonSerializer.Create(_serializerSettings).Serialize(streamWriter, debugInformationRoot);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<DebugInformationRoot> DeserializeAsync(string formattedText)
    {
        var debugInformationRoot =
            JsonConvert.DeserializeObject<DebugInformationRoot>(formattedText, _serializerSettings);
        return Task.FromResult(debugInformationRoot ?? throw new Exception("Deserialization failed"));
    }

    /// <inheritdoc />
    public Task<DebugInformationRoot> DeserializeAsync(Stream stream, CancellationToken? cancellationToken = null)
    {
        using var sr = new StreamReader(stream);
        using var jsonTextReader = new JsonTextReader(sr);
        var debugInformationRoot = JsonSerializer.Create(_serializerSettings).Deserialize<DebugInformationRoot>(jsonTextReader);
        return Task.FromResult(debugInformationRoot ?? throw new Exception("Deserialization failed"));
    }
}