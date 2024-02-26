using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;

/// <summary>
/// Object iterator node configuration
/// </summary>
public abstract class ObjectIteratorNodeConfiguration<TSignalConfigurationNode> : NodeConfiguration
    where TSignalConfigurationNode : TokenConfigurationNode
{
    /// <summary>
    /// List of transformations to apply to the signal
    /// </summary>
    public ICollection<TSignalConfigurationNode> Transformations { get; set; } = null!;
}

/// <summary>
/// Transform configuration node for one token
/// </summary>
public class TokenConfigurationNode : IChildNodeConfiguration
{
    /// <inheritdoc />
    public string? Description { get; set; }

    /// <inheritdoc />
    public ICollection<NodeConfiguration>? Transformations { get; set; }
}

/// <summary>
/// Object iterator node
/// </summary>
public abstract class ObjectIteratorNode<TTokenConfigurationNode>
    : ChildNodeBase where TTokenConfigurationNode : TokenConfigurationNode
{
    /// <summary>
    /// Processes the iterator
    /// </summary>
    /// <param name="dataContext"></param>
    /// <param name="nextDelegate"></param>
    /// <param name="iteratorConfigurationNode"></param>
    /// <exception cref="Exception"></exception>
    protected static async Task ProcessToken(IDataContext dataContext, NodeDelegate nextDelegate,
        TTokenConfigurationNode iteratorConfigurationNode)
    {
        if (dataContext.Current is JArray jArray)
        {
            var targetArray = new JArray();
            var tasks = new List<Task>();
            foreach (var jArrayToken in jArray)
            {
                var arrayContext = dataContext.Clone();
                arrayContext.Current = jArrayToken;

                var arrayNext = new NodeDelegate(d =>
                {
                    if (d.Current != null)
                    {
                        targetArray.Add(d.Current);
                    }

                    return Task.CompletedTask;
                });

                Task task = ProcessChildTransformationsAsSequenceAsync(arrayContext, arrayNext, iteratorConfigurationNode);
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            dataContext.Current = targetArray;
            await nextDelegate(dataContext);
        }
        else
        {
            var singleNext = new NodeDelegate(d =>
                {
                    dataContext.Current = d.Current;
                    return Task.CompletedTask;
                }
            );
            await ProcessChildTransformationsAsSequenceAsync(dataContext, singleNext, iteratorConfigurationNode);
            await nextDelegate(dataContext);
        }
    }

    //
    // /// <summary>
    // /// Runs the transforms
    // /// </summary>
    // /// <param name="dataContext"></param>
    // /// <param name="nextDelegate"></param>
    // /// <param name="iteratorConfigurationNode"></param>
    // /// <exception cref="Exception"></exception>
    // private static async Task RunTransforms(IDataContext dataContext, NodeDelegate nextDelegate, TTokenConfigurationNode iteratorConfigurationNode)
    // {
    //     if (iteratorConfigurationNode.Transformations == null)
    //     {
    //         await nextDelegate(dataContext);
    //         return;
    //     }
    //
    //     var nodeLookupService = dataContext.GlobalServiceProvider.GetRequiredService<INodeLookupService>();
    //
    //     foreach (var transformConfigurationNode in iteratorConfigurationNode.Transformations)
    //     {
    //         if (!nodeLookupService.TryGetNodeQualifiedName(transformConfigurationNode.GetType(), out var nodeQualifiedName))
    //         {
    //             throw DataPipelineException.UnknownConfigurationType(transformConfigurationNode.GetType());
    //         }
    //
    //         if (!nodeLookupService.TryCreateInstance(nodeQualifiedName, nextDelegate, out var node))
    //         {
    //             throw DataPipelineException.UnknownObjectPipelineNode(nodeQualifiedName);
    //         }
    //
    //         ((DataContext)dataContext).SetConfigurationNode(transformConfigurationNode);
    //         await node.ProcessObjectAsync(dataContext);
    //     }
    // }
}