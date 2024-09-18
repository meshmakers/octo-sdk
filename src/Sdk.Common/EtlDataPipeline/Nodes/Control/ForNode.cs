using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;

/// <summary>
/// Configuration for a for loop node.
/// </summary>
[NodeName("For", 1)]
public class ForNodeConfiguration : TargetPathNodeConfiguration, IChildNodeConfiguration
{
    /// <inheritdoc />
    public ForNodeConfiguration()
    {
        TargetValueKind = ValueKind.Simple;
    }
    
    /// <summary>
    /// The number of iterations
    /// </summary>
    public uint Count { get; set; }

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
        var c = dataContext.NodeContext.GetNodeConfiguration<ForNodeConfiguration>();
        var rootNodeContext = dataContext.NodeContext;
        var targetArray = new JArray();
#if NETSTANDARD2_0
        // ReSharper disable once AsyncVoidLambda
        Parallel.For(0, c.Count, async (index, _) => 
#else
        await Parallel.ForAsync<uint>(0, c.Count, async (index, _) =>
#endif
        {
            var arrayNext = new NodeDelegate(d =>
            {
                d.NodeContext.Complete(d);
                if (d.Current != null)
                {
                    targetArray.Add(d.Current);
                }

                return Task.CompletedTask;
            });

            var itemContext = dataContext.CreateChildContext(dataContext.Current?.DeepClone());
            if (!string.IsNullOrWhiteSpace(c.IndexTargetPath))
            {
                itemContext.SetValueByPath(c.IndexTargetPath, ValueKind.Simple, WriteMode.Overwrite, index);
            }
            var nodeContext = itemContext.RegisterChildNode(rootNodeContext, "", (uint)index, c);
            await ProcessChildTransformationsAsSequenceAsync(itemContext, arrayNext, c);
        });
        
        dataContext.SetValueByPath(c.TargetPath, c.TargetValueKind, c.TargetValueWriteMode, targetArray);
        await next(dataContext);
    }
}
