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
       // var tokens = dataContext.GetCurrentValuesByPath<JValue>(c.Path ?? "$");

        if (dataContext.Current is JArray jArray)
        {
            var targetArray = new JArray();
            var tasks = new List<Task>();
            for (var index = 0; index < jArray.Count; index++)
            {
                var jArrayToken = jArray[index];
                var index1 = index;

                async Task Function()
                {
                    var childNodePath = dataContext.NodeStack.Peek().Append(index1.ToString(), iteratorConfigurationNode.Description);
                    var arrayContext = new DataContext(dataContext, childNodePath, iteratorConfigurationNode)
                    {
                        Current = jArrayToken
                    };
                    
                    var arrayNext = new NodeDelegate(d =>
                    {
                        if (d.Current != null)
                        {
                            targetArray.Add(d.Current);
                        }

                        return Task.CompletedTask;
                    });
                    
                    dataContext.Logger.Debug(dataContext.NodeStack.Peek(), "Forward array index '{0}'", index1);
                    await ProcessChildTransformationsAsSequenceAsync(arrayContext, arrayNext, iteratorConfigurationNode);
                    dataContext.Logger.Debug(dataContext.NodeStack.Peek(), "Reverse array index '{0}'", index1);
                    arrayContext.PopNode();
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
                    dataContext.Current = d.Current;
                    return Task.CompletedTask;
                }
            );
            await ProcessChildTransformationsAsSequenceAsync(dataContext, singleNext, iteratorConfigurationNode);
            await nextDelegate(dataContext);
        }
    }
}