using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;

/// <summary>
/// Configuration for a loop node.
/// </summary>
[NodeName("Loop", 1)]
public class LoopNodeConfiguration : NodeConfiguration, IChildNodeConfiguration
{
    /// <summary>
    /// 
    /// </summary>
    public uint Iterations { get; set; }
    
    /// <summary>
    /// 
    /// </summary>
    public string? TargetPath { get; set; }
    
    /// <inheritdoc />
    public ICollection<NodeConfiguration>? Transformations { get; set; }
}

/// <summary>
/// Continuously processes the child nodes for a specified number of iterations.
/// </summary>
/// <param name="next"></param>
[NodeConfiguration(typeof(LoopNodeConfiguration))]
public class LoopNode(NodeDelegate next) : ChildNodeBase
{
    /// <inheritdoc />
    public override async Task ProcessObjectAsync(IDataContext dataContext)
    {
        var c = dataContext.GetNodeConfiguration<LoopNodeConfiguration>();
        
        var targetArray = new JArray();
        for (var i = 0; i < c.Iterations; i++)
        {            
            var arrayNext = new NodeDelegate(d =>
            {
                if (d.Current != null)
                {
                    targetArray.Add(d.Current);
                }

                return Task.CompletedTask;
            });
            
            await ProcessChildTransformationsAsSequenceAsync(dataContext, arrayNext, c);
        }
        
        dataContext.SetCurrentValueByPath(c.TargetPath, targetArray);
        await next(dataContext);
    }
}