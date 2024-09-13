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
        int index1 = 0;
        if (sourceArray != null)
        {
            foreach (var sourceToken in sourceArray.ToArray())
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
                    .Append(index1.ToString(), c.Description);
                var itemContext = new DataContext(dataContext, childNodePath, c)
                {
                    Current = sourceToken?.DeepClone()
                };
                await ProcessChildTransformationsAsSequenceAsync(itemContext, arrayNext, c);
                index1++;
            }
        }

        dataContext.SetCurrentValueByPath(c.TargetPath, targetArray);
        await next(dataContext);
    }
}