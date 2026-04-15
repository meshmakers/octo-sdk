using System.Collections.Concurrent;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.Services;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;

/// <summary>
/// Configuration for a for loop node.
/// </summary>
[NodeName("ForEach", 1)]
public record ForEachNodeConfiguration : SourceTargetPathNodeConfiguration, IChildNodeConfiguration
{
    /// <inheritdoc />
    public required ICollection<NodeConfiguration>? Transformations { get; set; }

    /// <summary>
    /// Gets or sets the path to the full document.
    /// </summary>
    [PropertyGroup("Paths", 2, "jsonpath")]
    public string FullDocumentPath { get; set; } = "$.full";

    /// <summary>
    /// Gets or sets the path to the array to iterate over.
    /// </summary>
    [PropertyGroup("Paths", 0, "jsonpath")]
    public required string IterationPath { get; set; }

    /// <summary>
    /// Gets or sets the path to the key of the current iteration.
    /// </summary>
    [PropertyGroup("Paths", 3, "jsonpath")]
    public string KeyPath { get; set; } = "$.key";

    /// <summary>
    /// Gets or sets the path used to merge the results of the child nodes.
    /// </summary>
    [PropertyGroup("Paths", 4, "jsonpath")]
    public string MergePath { get; set; } = "$.key";

    /// <summary>
    /// Gets or sets the maximum degree of parallelism for the loop.
    /// 0 = use Environment.ProcessorCount (default), -1 = unlimited, positive = explicit limit.
    /// </summary>
    [PropertyGroup("Execution", 10)]
    public int MaxDegreeOfParallelism { get; set; }
}

/// <summary>
/// Continuously processes the child nodes for each element in an array.
/// </summary>
/// <param name="next">The next node in the pipeline</param>
[NodeConfiguration(typeof(ForEachNodeConfiguration))]
public class ForEachNode(NodeDelegate next) : ChildNodeBase
{
    /// <inheritdoc />
    public override async Task ProcessObjectAsync(IDataContext dataContext, INodeContext rootNodeContext)
    {
        var c = rootNodeContext.GetNodeConfiguration<ForEachNodeConfiguration>();

        var subInputObject = dataContext.GetComplexObjectByPath<JToken>(c.Path);
        if (subInputObject == null)
        {
            throw PipelineExecutionException.PathNotFound(rootNodeContext.NodePath, c.Path);
        }
        var templateDataContext = new DataContext(dataContext, new JObject());
        templateDataContext.SetValueByPath(c.FullDocumentPath, DocumentModes.Extend, ValueKinds.Simple,
            TargetValueWriteModes.Overwrite, subInputObject);

        if (!dataContext.IsPathSimpleArrayValue(c.IterationPath))
        {
            throw PipelineExecutionException.PathMustBeArray(rootNodeContext.NodePath, nameof(c.IterationPath), c.IterationPath);
        }
        var sourceArray = dataContext.GetSimpleArrayValueByPath<JToken>(c.IterationPath);

        var targetArray = new ConcurrentBag<JToken>();
        if (sourceArray != null)
        {
            var copyArray = sourceArray.ToArray();

            var maxDop = c.MaxDegreeOfParallelism switch
            {
                0 => Environment.ProcessorCount,
                -1 => -1,
                _ => c.MaxDegreeOfParallelism
            };

#if NETSTANDARD2_0
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = maxDop };
            // ReSharper disable once AsyncVoidLambda
            Parallel.For(0, copyArray.Length, parallelOptions, async (i, _) =>
            {
                var index = (uint)i;
#else
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = maxDop };
            await Parallel.ForAsync<uint>(0, (uint)copyArray.Length, parallelOptions, async (index, _) =>
            {
#endif
                var sourceToken = copyArray[index];
                var itemDataContext = dataContext.CreateChildDataContext(templateDataContext.Current ?? new JObject());
                itemDataContext.SetValueByPath(c.KeyPath, DocumentModes.Extend, ValueKinds.Simple, TargetValueWriteModes.Overwrite, sourceToken);

                var itemNodeContext = rootNodeContext.RegisterChildNode(index, c,
                    itemDataContext);
                var arrayNext = new NodeDelegate((ds, nc) =>
                {
                    itemNodeContext.Unregister(ds);
                    var mergeItem = ds.GetComplexObjectByPath<JToken>(c.MergePath);
                    if (mergeItem != null)
                    {
                        targetArray.Add(mergeItem);
                    }

                    return Task.CompletedTask;
                });

                await ProcessChildTransformationsAsSequenceAsync(itemDataContext, itemNodeContext, arrayNext, c);
            });
        }

        dataContext.SetValueByPath(c.TargetPath, c.DocumentMode, c.TargetValueKind, c.TargetValueWriteMode,
            JArray.FromObject(targetArray));

        await next(dataContext, rootNodeContext);
    }
}
