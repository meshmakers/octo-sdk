using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;

/// <summary>
/// Configuration for assign object node.
/// </summary>
[NodeName("SelectByPath", 1)]
public class SelectByPathNodeConfiguration : ObjectIteratorNodeConfiguration<PathPropertyConfigurationNode>;

/// <summary>
/// Contains transformation information of a property.
/// </summary>
public class PathPropertyConfigurationNode : TokenConfigurationNode
{
    /// <summary>
    /// Gets or sets the source path in json path format
    /// </summary>
    public string Path { get; set; } = $"$";
    
    /// <summary>
    /// Gets or sets the target path in json path format
    /// </summary>
    public string TargetPath { get; set; } = "$";
    
    /// <summary>
    /// Gets or sets the write mode (overwrite, append, prepend)
    /// </summary>
    public WriteMode TargetValueWriteMode { get; set; } = WriteMode.Overwrite;

    /// <summary>
    /// Gets or sets the value kind to write (simple value or array)
    /// </summary>
    public ValueKind TargetValueKind { get; set; } = ValueKind.Simple;
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
        var c = dataContext.NodeContext.GetNodeConfiguration<SelectByPathNodeConfiguration>();

        if (dataContext.Current != null)
        {
            var rootNodeContext = dataContext.NodeContext;
            var tasks = new List<Task>();
            foreach (var selectPath in c.SelectPath)
            {
                var path = selectPath.Path;
                var jToken = dataContext.Current.SelectToken(path);

                var tokenNextDelegate = new NodeDelegate(d =>
                {
                    dataContext.Current ??= new JObject();
                    dataContext.SetValueByPath(selectPath.TargetPath, selectPath.TargetValueKind, selectPath.TargetValueWriteMode, d.Current);
                    return Task.CompletedTask;
                });
              
                async Task Function()
                {
                    var pathContext = dataContext.CreateChildContext(jToken?.DeepClone());
                    var nodeContext = pathContext.RegisterChildNode(rootNodeContext, path, 0, selectPath);
                    nodeContext.Debug("Forward handling path");
                    await ProcessToken(pathContext, tokenNextDelegate, selectPath);
                    nodeContext.Debug("Reverse handling path completed");
                }
                
                tasks.Add(Task.Run((Func<Task>)Function));
            }
            
            await Task.WhenAll(tasks);
        }

        await next(dataContext);
    }
}