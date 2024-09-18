using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
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
    public ICollection<TSignalConfigurationNode> SelectPath { get; set; } = null!;
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
            var rootNodeContext = dataContext.NodeContext;
            var targetArray = new JArray();
            var tasks = new List<Task>();
            for (int index = 0; index < jArray.Count; index++)
            {
                var jArrayToken = jArray[index];
                int index1 = index;

                async Task Function()
                {
                    var (itemContext, nodeContext) = dataContext.CreateSubContext(jArrayToken?.DeepClone(),  rootNodeContext,"", (uint)index1, iteratorConfigurationNode);

                    var arrayNext = new NodeDelegate(d =>
                    {
                        d.NodeContext.Complete(d);
                        if (d.Current != null)
                        {
                            targetArray.Add(d.Current);
                        }

                        return Task.CompletedTask;
                    });
                    
                    nodeContext.Debug("Forward array index '{0}'", index1);
                    await ProcessChildTransformationsAsSequenceAsync(itemContext, arrayNext, iteratorConfigurationNode);
                    nodeContext.Debug("Reverse array index '{0}'", index1);
                }

                tasks.Add(Task.Run((Func<Task>)Function));
            }

            await Task.WhenAll(tasks);

            dataContext.Current = targetArray;
            await nextDelegate(dataContext);
        }
        else
        {
            var singleNext = new NodeDelegate(d =>
                {
                    d.NodeContext.Complete(d);
                    dataContext.Current = d.Current;
                    return Task.CompletedTask;
                }
            );
            await ProcessChildTransformationsAsSequenceAsync(dataContext, singleNext, iteratorConfigurationNode);
            await nextDelegate(dataContext);
        }
    }
}