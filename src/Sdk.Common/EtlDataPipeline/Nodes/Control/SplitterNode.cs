using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;

/// <summary>
/// Configuration for a assign object node.
/// </summary>
public class SplitterNodeConfiguration : NodeConfiguration, IChildNodeConfiguration
{
    /// <inheritdoc />
    public ICollection<NodeConfiguration>? Transformations { get; set; }
}

/// <summary>
/// Split data from a single source into multiple nodes.
/// </summary>
[Node("Splitter", 1, typeof(SplitterNodeConfiguration))]
public class SplitterNode(NodeDelegate next) : IPipelineNode
{
    /// <inheritdoc />
    public async Task ProcessObjectAsync(IDataContext dataContext)
    {
        var c = dataContext.GetNodeConfiguration<SplitterNodeConfiguration>();
        dataContext.Logger.LogDebug("Executing {Node} {Description}", nameof(SplitterNode), c.Description);

        var nodeLookupService = dataContext.GlobalServiceProvider.GetRequiredService<INodeLookupService>();
        
        if (c.Transformations == null)
        {
            dataContext.Logger.LogDebug("Executing {Node} {Description} - transformation null executing next", nameof(SplitterNode), c.Description);
            await next(dataContext);
            return;
        }

        var tasks = new List<Task>();
        var objectList = new List<JToken?>();
        foreach (var nodeConfiguration in c.Transformations)
        {
            if (!nodeLookupService.TryGetNodeConfigurationQualifiedName(nodeConfiguration.GetType(), out var nodeQualifiedName))
            {
                throw DataPipelineException.UnknownConfigurationType(nodeConfiguration.GetType());
            }
            
            var nextDelegate = new NodeDelegate(d =>
            {
                objectList.Add(d.Current);
                return Task.CompletedTask;
            });

            if (!nodeLookupService.TryCreateInstance(nodeQualifiedName, nextDelegate, out var node))
            {
                throw DataPipelineException.UnknownObjectPipelineNode(nodeQualifiedName);
            }

            var clone = dataContext.Clone();
            ((DataContext)dataContext).SetConfigurationNode(nodeConfiguration);
            tasks.Add(node.ProcessObjectAsync(clone));
        }

        await Task.WhenAll(tasks);
        
        var dataContextNext = dataContext.Clone();
        dataContextNext.Current = JsonConvert.SerializeObject(objectList);
        dataContext.Logger.LogDebug("Executing {Node} {Description} done - executing next", nameof(SplitterNode), c.Description);
        await next(dataContextNext);
    }
}