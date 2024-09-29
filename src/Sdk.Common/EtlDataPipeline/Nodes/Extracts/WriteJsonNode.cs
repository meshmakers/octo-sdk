using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Extracts;

/// <summary>
/// Configuration for the WriteJsonNode
/// </summary>
[NodeName("WriteJson", 1)]
public record WriteJsonConfiguration : TargetPathNodeConfiguration
{
    /// <summary>
    /// The json string to write to the current object
    /// </summary>
    public required string JsonString { get; init; } = null!;
}

/// <summary>
/// Writes a json string to the current object
/// </summary>
/// <param name="next"></param>
[NodeConfiguration(typeof(WriteJsonConfiguration))]
// ReSharper disable once ClassNeverInstantiated.Global
public class WriteJsonNode(NodeDelegate next) : IPipelineNode
{
    /// <inheritdoc />
    public Task ProcessObjectAsync(IDataContext dataContext)
    {
        var c = dataContext.NodeContext.GetNodeConfiguration<WriteJsonConfiguration>();
        
        dataContext.SetValueByPath(c.TargetPath, c.TargetValueKind, c.TargetValueWriteMode, JObject.Parse(c.JsonString));

        return next(dataContext);
    }
}