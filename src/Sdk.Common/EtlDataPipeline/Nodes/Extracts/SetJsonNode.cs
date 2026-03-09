using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Extracts;

/// <summary>
/// Configuration for the WriteJsonNode
/// </summary>
[NodeName("WriteJson", 1)]
public record SetJsonNodeConfiguration : TargetPathNodeConfiguration
{
    /// <summary>
    /// The json string to write to the current object
    /// </summary>
    public required string JsonString { get; init; } = null!;
}

/// <summary>
/// Sets a json string to the current object
/// </summary>
/// <param name="next">Next node in the pipeline</param>
[NodeConfiguration(typeof(SetJsonNodeConfiguration))]
// ReSharper disable once ClassNeverInstantiated.Global
public class SetJsonNode(NodeDelegate next) : IPipelineNode
{
    /// <inheritdoc />
    public Task ProcessObjectAsync(IDataContext dataContext, INodeContext nodeContext)
    {
        var c = nodeContext.GetNodeConfiguration<SetJsonNodeConfiguration>();

        dataContext.SetValueByPath(c.TargetPath, c.DocumentMode, c.TargetValueKind, c.TargetValueWriteMode,
            JToken.Parse(c.JsonString));

        return next(dataContext, nodeContext);
    }
}