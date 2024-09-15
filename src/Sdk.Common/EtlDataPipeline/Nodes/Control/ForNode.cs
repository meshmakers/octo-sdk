using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;

/// <summary>
/// Configuration for a for loop node.
/// </summary>
[NodeName("For", 1)]
public class ForNodeConfiguration : NodeConfiguration, IChildNodeConfiguration
{
    /// <summary>
    /// The number of iterations
    /// </summary>
    public uint Count { get; set; }
    
    /// <summary>
    /// Path the result is stored as array.
    /// </summary>
    public string? TargetPath { get; set; }
    
    /// <summary>
    /// Path the index of the current iteration is stored.
    /// </summary>
    public string? IndexTargetPath { get; set; }
    
    /// <inheritdoc />
    public ICollection<NodeConfiguration>? Transformations { get; set; }
}

/// <summary>
/// Continuously processes the child nodes for a specified number of iterations.
/// </summary>
/// <param name="next">The next node in the pipeline</param>
[NodeConfiguration(typeof(ForNodeConfiguration))]
// ReSharper disable once ClassNeverInstantiated.Global
public class ForNode(NodeDelegate next) : ChildNodeBase
{
    /// <inheritdoc />
    public override async Task ProcessObjectAsync(IDataContext dataContext)
    {
        var c = dataContext.GetNodeConfiguration<ForNodeConfiguration>();
        
        var targetArray = new JArray();
#if NETSTANDARD2_0
        Parallel.For(0, c.Count, async (index, _) => 
#else
        await Parallel.ForAsync<uint>(0, c.Count, async (index, _) =>
#endif
        {
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
            // ReSharper disable once RedundantCast
            var itemContext = new DataContext(dataContext, childNodePath, (uint)index, c)
            {
                Current = dataContext.Current?.DeepClone()
            };
            if (!string.IsNullOrWhiteSpace(c.IndexTargetPath))
            {
                itemContext.SetCurrentValueByPath(c.IndexTargetPath, index);
            }

            await ProcessChildTransformationsAsSequenceAsync(itemContext, arrayNext, c);
        });
        
        dataContext.SetCurrentValueByPath(c.TargetPath, targetArray);
        await next(dataContext);
    }
}