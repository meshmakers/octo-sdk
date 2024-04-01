using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;

/// <summary>
/// Configuration for a assign object node.
/// </summary>
[NodeName("SelectByPath", 1)]
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
[NodeConfiguration(typeof(SelectByPathNodeConfiguration))]
public class SelectByPathNode(NodeDelegate next) : ObjectIteratorNode<PathPropertyConfigurationNode>
{
    /// <inheritdoc />
    public override async Task ProcessObjectAsync(IDataContext dataContext)
    {
        var c = dataContext.GetNodeConfiguration<SelectByPathNodeConfiguration>();

        if (dataContext.Current != null)
        {
            var tasks = new List<Task>();
            foreach (var tn in c.Transformations)
            {
                var path = tn.SourcePath ?? "$";
                var jToken = dataContext.Current.SelectToken(path);

                var tokenNextDelegate = new NodeDelegate(d =>
                {
                    dataContext.Current ??= new JObject();
                    dataContext.SetCurrentValueByPath(tn.TargetPropertyName, d.Current);
                    return Task.CompletedTask;
                });
              
                async Task Function()
                {
                    var childNodePath = dataContext.NodeStack.Peek().Append(path, tn.Description);
                    var pathDataContext = new DataContext(dataContext, childNodePath, tn)
                    {
                        Current = jToken
                    };
                    pathDataContext.Logger.Debug(childNodePath, $"Forward handling path");
                    await ProcessToken(pathDataContext, tokenNextDelegate, tn);
                    pathDataContext.Logger.Debug(childNodePath, $"Reverse handling path completed.");
                    pathDataContext.PopNode();
                };
                
                tasks.Add(Task.Run((Func<Task>)Function));
            }
            
            await Task.WhenAll(tasks);
        }

        await next(dataContext);
    }
}