using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;

/// <summary>
/// Configuration for a assign object node.
/// </summary>
[NodeName("Splitter", 1)]
public class SplitterNodeConfiguration : NodeConfiguration, IChildNodeConfiguration
{
    /// <inheritdoc />
    public ICollection<NodeConfiguration>? Transformations { get; set; }
}

/// <summary>
/// Split data from a single source into multiple nodes.
/// </summary>
[NodeConfiguration(typeof(SplitterNodeConfiguration))]
public class SplitterNode(NodeDelegate next) : IPipelineNode
{
    /// <inheritdoc />
    public async Task ProcessObjectAsync(IDataContext dataContext)
    {
        var c = dataContext.GetNodeConfiguration<SplitterNodeConfiguration>();

        var nodeLookupService = dataContext.GlobalServiceProvider.GetRequiredService<INodeLookupService>();

        if (c.Transformations == null)
        {
            dataContext.Logger.Debug(dataContext.NodeStack.Peek(), "No transformations to process");
            await next(dataContext);
            return;
        }

        var tasks = new List<Task>();
        var objectList = new List<JToken?>();
        foreach (var nodeConfiguration in c.Transformations)
        {
            if (!nodeLookupService.TryGetNodeConfigurationQualifiedName(nodeConfiguration.GetType(),
                    out var nodeQualifiedName))
            {
                throw DataPipelineException.UnknownConfigurationType(nodeConfiguration.GetType());
            }

            var nextDelegate = new NodeDelegate(d =>
            {
                objectList.Add(d.Current);
                return Task.CompletedTask;
            });

            if (!nodeLookupService.TryCreateInstance(dataContext.GlobalServiceProvider, nodeQualifiedName!, nextDelegate,
                    out var node))
            {
                throw DataPipelineException.UnknownObjectPipelineNode(nodeQualifiedName!);
            }
          
            async Task Function()
            {
                var childNodePath = dataContext.NodeStack.Peek().Append(nodeQualifiedName!, nodeConfiguration.Description);
                var clone = new DataContext(dataContext, childNodePath, nodeConfiguration);
                clone.Logger.Debug(childNodePath, $"Executing");
                await node!.ProcessObjectAsync(clone);
                clone.Logger.Debug(childNodePath, $"Execution completed");
                clone.PopNode();
            }

            tasks.Add(Task.Run((Func<Task>)Function));
        }

        await Task.WhenAll(tasks);

        dataContext.Current = JsonConvert.SerializeObject(objectList);
        await next(dataContext);
    }
}