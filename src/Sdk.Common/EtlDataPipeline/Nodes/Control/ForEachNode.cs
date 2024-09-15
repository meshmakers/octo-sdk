using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;

/// <summary>
/// Configuration for a for loop node.
/// </summary>
[NodeName("ForEach", 1)]
public class ForEachNodeConfiguration : NodeConfiguration, IChildNodeConfiguration
{
    /// <summary>
    /// The path an array is selected from.
    /// </summary>
    public string? Path { get; set; }
    
    /// <summary>
    /// Path the result is stored as array.
    /// </summary>
    public string? TargetPath { get; set; }
    
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
        var c = dataContext.GetNodeConfiguration<ForEachNodeConfiguration>();
        
        var sourceArray = dataContext.GetCurrentValuesByPath<JToken>(c.Path ?? "$");
        
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

                var childNodePath = dataContext.NodeStack.Peek()
                    .Append(index.ToString());
                var itemContext = new DataContext(dataContext, childNodePath, (uint)index, c)
                {
                    Current = sourceToken?.DeepClone()
                };
                await ProcessChildTransformationsAsSequenceAsync(itemContext, arrayNext, c);
            });
        }

        dataContext.SetCurrentValueByPath(c.TargetPath, targetArray);
        await next(dataContext);
    }
}