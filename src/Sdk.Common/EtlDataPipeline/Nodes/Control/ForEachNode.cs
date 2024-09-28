using System.Collections.Concurrent;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;

/// <summary>
/// Configuration for a for loop node.
/// </summary>
[NodeName("ForEach", 1)]
public record ForEachNodeConfiguration : SourceTargetPathNodeConfiguration, IChildNodeConfiguration
{
    /// <inheritdoc />
    public required ICollection<NodeConfiguration>? Transformations { get; set; }
}

/// <summary>
/// Continuously processes the child nodes for each element in an array.
/// </summary>
/// <param name="next">The next node in the pipeline</param>
[NodeConfiguration(typeof(ForEachNodeConfiguration))]
public class ForEachNode(NodeDelegate next) : ChildNodeBase
{
    /// <inheritdoc />
    public override async Task ProcessObjectAsync(IDataContext dataContext)
    {
        var c = dataContext.NodeContext.GetNodeConfiguration<ForEachNodeConfiguration>();
        var rootNodeContext = dataContext.NodeContext;
        var sourceArray = dataContext.GetSimpleArrayValueByPath<JToken>(c.Path);

        var targetArray = new ConcurrentBag<JToken>();
        if (sourceArray != null)
        {
            var copyArray = sourceArray.ToArray();

#if NETSTANDARD2_0
            Parallel.For(0, copyArray.Length, async (index, _) =>
#else
            await Parallel.ForAsync<uint>(0, (uint)copyArray.Length, async (index, _) =>
#endif
            {
                var sourceToken = copyArray[index];

                
                var (itemContext, itemNodeContext) = dataContext.CreateSubContext(sourceToken?.DeepClone(),  rootNodeContext,"", (uint)index, c);
                var arrayNext = new NodeDelegate(d =>
                {
                    itemNodeContext.Complete(d);
                    if (d.Current != null)
                    {
                        targetArray.Add(d.Current);
                    }

                    return Task.CompletedTask;
                });

                await ProcessChildTransformationsAsSequenceAsync(itemContext, arrayNext, c);
            });
        }
        dataContext.SetValueByPath(c.TargetPath, c.TargetValueKind, c.TargetValueWriteMode, JArray.FromObject(targetArray));
        await next(dataContext);
    }
}