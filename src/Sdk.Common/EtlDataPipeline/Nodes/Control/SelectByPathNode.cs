using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;

/// <summary>
/// Configuration for a assign object node.
/// </summary>
public class SelectByPathNodeConfiguration : ObjectIteratorNodeConfiguration<PathPropertyConfigurationNode>;

/// <summary>
/// Contains transformation information of a property.
/// </summary>
public class PathPropertyConfigurationNode : TokenConfigurationNode
{
    /// <summary>
    /// Source path using JSONPath.
    /// </summary>
    public string? SourcePath { get; set; }

    /// <summary>
    /// The target property name.
    /// </summary>
    public string? TargetPropertyName { get; set; }
}

/// <summary>
/// Transforms a list of properties from the source.
/// </summary>
[Node("SelectByPath", 1, typeof(SelectByPathNodeConfiguration))]
public class SelectByPathNode(NodeDelegate next) : ObjectIteratorNode<PathPropertyConfigurationNode>
{
    /// <inheritdoc />
    public override async Task ProcessObjectAsync(IDataContext dataContext)
    {
        var c = dataContext.GetNodeConfiguration<SelectByPathNodeConfiguration>();
        dataContext.Logger.LogDebug("Executing {Node} {Description}", nameof(SelectByPathNode), c.Description);

        if (dataContext.Current != null)
        {
            var tasks = new List<Task>();
            foreach (var tn in c.Transformations)
            {
                var jToken = dataContext.Current.SelectToken(tn.SourcePath ?? "$");

                var tokenNextDelegate = new NodeDelegate(d =>
                {
                    dataContext.Current ??= new JObject();
                    dataContext.SetCurrentValueByPath(tn.TargetPropertyName, d.Current);
                    return Task.CompletedTask;
                });
                
                var pathDataContext = dataContext.Clone();
                pathDataContext.Current = jToken;
                tasks.Add(ProcessToken(pathDataContext, tokenNextDelegate, tn));
            }
            
            await Task.WhenAll(tasks);
        }

        dataContext.Logger.LogDebug("Executing {Node} {Description} done - executing next", nameof(SelectByPathNode), c.Description);
        await next(dataContext);
    }
}