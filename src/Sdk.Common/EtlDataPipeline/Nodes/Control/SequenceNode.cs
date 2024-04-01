using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;

/// <summary>
/// Configuration for a assign object node.
/// </summary>
[NodeName("Sequence", 1)]
public class SequenceNodeConfiguration : NodeConfiguration, IChildNodeConfiguration
{
    /// <inheritdoc />
    public ICollection<NodeConfiguration>? Transformations { get; set; }
}

/// <summary>
/// Split data from a single source into multiple nodes.
/// </summary>
[NodeConfiguration(typeof(SequenceNodeConfiguration))]
public class SequenceNode(NodeDelegate next) : ChildNodeBase
{
    /// <inheritdoc />
    public override async Task ProcessObjectAsync(IDataContext dataContext)
    {
        var c = dataContext.GetNodeConfiguration<SequenceNodeConfiguration>();

        await ProcessChildTransformationsAsSequenceAsync(dataContext, next, c);
    }
}