using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;

/// <summary>
/// Configuration for a for loop node.
/// </summary>
[NodeName("ForEach", 1)]
public class ForEachNodeConfiguration : SourceTargetPathNodeConfiguration, IChildNodeConfiguration
{
    /// <inheritdoc />
    public ICollection<NodeConfiguration>? Transformations { get; set; }
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
        
        var sourceArray = dataContext.GetSimpleArrayValueByPath<JToken>(c.Path);
        
        var targetArray = new JArray();
        if (sourceArray != null)
        {
            var copyArray = sourceArray.ToArray();
            
#if NETSTANDARD2_0
            Parallel.For(0, copyArray.Length, async (index, _) => 
#else
            await Parallel.ForAsync(0, copyArray.Length, async (index, _) => 
#endif            
            {
                var sourceToken = copyArray[index];
                
                var arrayNext = new NodeDelegate(d =>
                {
                    if (d.Current != null)
                    {
                        targetArray.Add(d.Current);
                    }

                    return Task.CompletedTask;
                });

                var itemContext = dataContext.CreateChildContext(sourceToken?.DeepClone());
                await ProcessChildTransformationsAsSequenceAsync(itemContext, arrayNext, c);
            });
        }

        dataContext.SetValueByPath(c.TargetPath, c.TargetValueKind, c.TargetValueWriteMode, targetArray);
        await next(dataContext);
    }
}