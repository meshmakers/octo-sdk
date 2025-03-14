using System.Collections.Concurrent;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;

/// <summary>
/// Configuration for a for loop node.
/// </summary>
[NodeName("For", 1)]
public record ForNodeConfiguration : SourceTargetPathNodeConfiguration, IChildNodeConfiguration
{
    /// <inheritdoc />
    public ForNodeConfiguration()
    {
        TargetValueKind = ValueKinds.Simple;
    }

    /// <summary>
    /// The number of iterations
    /// </summary>
    public required uint Count { get; set; }

    /// <summary>
    /// Path the index of the current iteration is stored.
    /// </summary>
    public string? IndexTargetPath { get; set; }

    /// <inheritdoc />
    public required ICollection<NodeConfiguration>? Transformations { get; set; }
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
    public override async Task ProcessObjectAsync(IDataContext dataContext, INodeContext rootNodeContext)
    {
        var c = rootNodeContext.GetNodeConfiguration<ForNodeConfiguration>();
        var subInputObject = dataContext.GetComplexObjectByPath<JToken>(c.Path) ?? new JObject();
        var targetArray = new ConcurrentBag<JToken>();

#if NETSTANDARD2_0
        // ReSharper disable once AsyncVoidLambda
        Parallel.For(0, c.Count, async (i, _) =>
        {
            var index = (uint)i;
#else
        await Parallel.ForAsync<uint>(0, c.Count, async (index, _) =>
        {
#endif
            var (itemDataContext, itemNodeContext) =
                rootNodeContext.CreateSubContext(subInputObject, index, c, dataContext);

            var arrayNext = new NodeDelegate((dc, nc) =>
            {
                itemNodeContext.Unregister(dc);
                if (dc.Current != null)
                {
                    targetArray.Add(dc.Current);
                }

                return Task.CompletedTask;
            });


            if (!string.IsNullOrWhiteSpace(c.IndexTargetPath))
            {
                itemDataContext.SetValueByPath(c.IndexTargetPath, DocumentModes.Extend, ValueKinds.Simple,
                    TargetValueWriteModes.Overwrite, index);
            }

            await ProcessChildTransformationsAsSequenceAsync(itemDataContext, itemNodeContext, arrayNext, c);
        });

        dataContext.SetValueByPath(c.TargetPath, c.DocumentMode, c.TargetValueKind, c.TargetValueWriteMode,
            JArray.FromObject(targetArray));

        await next(dataContext, rootNodeContext);
    }
}