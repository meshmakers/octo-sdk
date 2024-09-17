using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;

/// <summary>
/// Child node configuration
/// </summary>
public interface IChildNodeConfiguration : INodeConfiguration
{
    /// <summary>
    /// Child transformations of the current node
    /// </summary>
    ICollection<NodeConfiguration>? Transformations { get; set; }
}

/// <summary>
/// A node with child transformations
/// </summary>
public abstract class ChildNodeBase : IPipelineNode
{
    /// <summary>
    /// Processes the child transformations
    /// </summary>
    /// <param name="dataContext"></param>
    /// <param name="next"></param>
    /// <param name="c"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    protected static async Task ProcessChildTransformationsAsSequenceAsync(IDataContext dataContext, NodeDelegate next,
        IChildNodeConfiguration c)
    {
        if (c.Transformations == null)
        {
            await next(dataContext);
            return;
        }

        var nodeLookupService = dataContext.GlobalServiceProvider.GetRequiredService<INodeLookupService>();

        // This is the last delegate in the sequence -> it will call the next node in the pipeline
        var nextDelegate = new NodeDelegate(async d => await next(d));

        foreach (var nodeConfiguration in c.Transformations.Reverse())
        {
            if (!nodeLookupService.TryGetNodeConfigurationQualifiedName(nodeConfiguration.GetType(),
                    out var nodeQualifiedName))
            {
                throw DataPipelineException.UnknownConfigurationType(nodeConfiguration.GetType());
            }

            if (!nodeLookupService.TryCreateInstance(dataContext.GlobalServiceProvider, nodeQualifiedName!, nextDelegate,
                    out var node))
            {
                throw DataPipelineException.UnknownObjectPipelineNode(nodeQualifiedName!);
            }

            var rootNodeContext = dataContext.NodeContext;

            // This is the next delegate in the sequence -> it will call the next node in the sequence            
            nextDelegate = async d =>
            {
                var nodeContext = d.RegisterChildNode(rootNodeContext, nodeQualifiedName!, dataContext.NodeContext.SequenceNumber + 1,
                    nodeConfiguration);
                nodeContext.Debug("Forward Executing (child)");
                await node!.ProcessObjectAsync(dataContext);
                nodeContext.Debug("Reverse completed (child)");
                nodeContext.Complete(d);
            };
        }

        await nextDelegate(dataContext);
    }

    /// <inheritdoc />
    public abstract Task ProcessObjectAsync(IDataContext dataContext);
}