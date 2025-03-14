using System.Collections.Concurrent;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;

/// <summary>
/// Configuration for assign object node.
/// </summary>
[NodeName("SelectByPath", 1)]
public record SelectByPathNodeConfiguration : ObjectIteratorNodeConfiguration<PathPropertyConfigurationNode>;

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
    public TargetValueWriteModes TargetValueTargetValueWriteMode { get; set; } = TargetValueWriteModes.Overwrite;

    /// <summary>
    /// Gets or sets the value kind to write (simple value or array)
    /// </summary>
    public ValueKinds TargetValueKind { get; set; } = ValueKinds.Simple;

    /// <summary>
    /// Gets or sets the document mode (extend, replace)
    /// </summary>
    public DocumentModes DocumentMode { get; set; } = DocumentModes.Extend;
}

/// <summary>
/// Transforms a list of properties from the source.
/// </summary>
[NodeConfiguration(typeof(SelectByPathNodeConfiguration))]
public class SelectByPathNode(NodeDelegate next) : ChildNodeBase
{
    private record UpdateItem
    {
        public required string TargetPath { get; init; }
        public required DocumentModes DocumentMode { get; init; }
        public required ValueKinds TargetValueKind { get; init; }
        public required TargetValueWriteModes TargetValueWriteMode { get; init; }
        public required JToken? Value { get; init; }
    }

    /// <inheritdoc />
    public override async Task ProcessObjectAsync(IDataContext dataContext, INodeContext rootNodeContext)
    {
        var c = rootNodeContext.GetNodeConfiguration<SelectByPathNodeConfiguration>();

        if (dataContext.Current != null)
        {
            var tasks = new List<Task>();
            var updateItems = new ConcurrentBag<UpdateItem>();
            foreach (var selectPath in c.SelectPath)
            {
                var path = selectPath.Path;
                var jToken = dataContext.Current.SelectToken(path);
                if (jToken == null)
                {
                    rootNodeContext.Debug($"No token found for path: {path}. Skipping execution.");
                    continue;
                }

                var tokenNextDelegate = new NodeDelegate((ds, nc) =>
                {
                    nc.Unregister(ds);

                    updateItems.Add(new UpdateItem
                    {
                        TargetPath = selectPath.TargetPath,
                        DocumentMode = selectPath.DocumentMode,
                        TargetValueKind = selectPath.TargetValueKind,
                        TargetValueWriteMode = selectPath.TargetValueTargetValueWriteMode,
                        Value = ds.Current
                    });
                    return Task.CompletedTask;
                });

                async Task Function()
                {
                    var (pathContext, pathNodeContext) = rootNodeContext.CreateSubContext(jToken.DeepClone(),
                        path, selectPath, dataContext);
                    pathNodeContext.Debug("Forward handling path");
                    await ProcessChildTransformationsAsSequenceAsync(pathContext, pathNodeContext, tokenNextDelegate,
                        selectPath);
                    pathNodeContext.Debug("Reverse handling path completed");
                }

                tasks.Add(Task.Run((Func<Task>)Function));
            }

            await Task.WhenAll(tasks);

            foreach (var updateItem in updateItems)
            {
                dataContext.SetValueByPath(updateItem.TargetPath, updateItem.DocumentMode, updateItem.TargetValueKind,
                    updateItem.TargetValueWriteMode, updateItem.Value);
            }
        }

        await next(dataContext, rootNodeContext);
    }
}