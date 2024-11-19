using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Extracts;


/// <summary>
/// Node configuration to set a primitive value on the current object
/// </summary>
[NodeName("SetPrimitiveValue", 1)]
// ReSharper disable once ClassNeverInstantiated.Global
public record SetPrimitiveValueNodeConfiguration : TargetPathNodeConfiguration
{
    /// <summary>
    /// The primitive value to set
    /// </summary>
    public required object Value { get; init; } = null!;
}

/// <summary>
/// Sets a primitive value on the current object
/// </summary>
/// <param name="next">Next node in the pipeline</param>
[NodeConfiguration(typeof(SetPrimitiveValueNodeConfiguration))]
// ReSharper disable once ClassNeverInstantiated.Global
public class SetPrimitiveValueNode(NodeDelegate next) : IPipelineNode
{
    /// <inheritdoc />
    public Task ProcessObjectAsync(IDataContext dataContext)
    {
        var c = dataContext.NodeContext.GetNodeConfiguration<SetPrimitiveValueNodeConfiguration>();
        
        dataContext.SetValueByPath(c.TargetPath, c.TargetValueKind, c.TargetValueWriteMode, c.Value);

        return next(dataContext);
    }
}