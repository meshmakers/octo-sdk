using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Microsoft.Extensions.Logging;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;

/// <summary>
/// Configuration for a assign object node.
/// </summary>
public class SequenceNodeConfiguration : NodeConfiguration, IChildNodeConfiguration
{
    /// <inheritdoc />
    public ICollection<NodeConfiguration>? Transformations { get; set; }
}

/// <summary>
/// Split data from a single source into multiple nodes.
/// </summary>
[Node("Sequence", 1, typeof(SequenceNodeConfiguration))]
public class SequenceNode(NodeDelegate next) : ChildNodeBase
{
    /// <inheritdoc />
    public override async Task ProcessObjectAsync(IDataContext dataContext)
    {
        var c = dataContext.GetNodeConfiguration<SequenceNodeConfiguration>();
        dataContext.Logger.LogDebug("Executing {Node} {Description}", nameof(SequenceNode), c.Description);

        await ProcessChildTransformationsAsSequenceAsync(dataContext, next, c);
        dataContext.Logger.LogDebug("Executing {Node} {Description} done", nameof(SequenceNode), c.Description);
    }
}