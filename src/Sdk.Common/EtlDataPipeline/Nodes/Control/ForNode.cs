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
    /// The number of iterations (static value). Used when <see cref="CountPath"/> is not set.
    /// </summary>
    [PropertyGroup("Options", 0)]
    public uint Count { get; set; }

    /// <summary>
    /// JSON path to dynamically resolve the iteration count from the data context.
    /// Takes precedence over <see cref="Count"/> when set.
    /// </summary>
    [PropertyGroup("Paths", 2, "jsonpath")]
    public string? CountPath { get; set; }

    /// <summary>
    /// Path the index of the current iteration is stored.
    /// </summary>
    [PropertyGroup("Paths", 3, "jsonpath")]
    public string? IndexTargetPath { get; set; }

    /// <inheritdoc />
    public required ICollection<NodeConfiguration>? Transformations { get; set; }

    /// <summary>
    /// Gets or sets the maximum degree of parallelism for the loop.
    /// 0 = use Environment.ProcessorCount (default), -1 = unlimited, positive = explicit limit.
    /// </summary>
    [PropertyGroup("Execution", 10)]
    public int MaxDegreeOfParallelism { get; set; }
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
        var count = ResolveCount(dataContext, c);
        var subInputObject = dataContext.GetComplexObjectByPath<JToken>(c.Path) ?? new JObject();
        var targetArray = new ConcurrentBag<JToken>();

        var maxDop = c.MaxDegreeOfParallelism switch
        {
            0 => Environment.ProcessorCount,
            -1 => -1,
            _ => c.MaxDegreeOfParallelism
        };

#if NETSTANDARD2_0
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = maxDop };
        // ReSharper disable once AsyncVoidLambda
        Parallel.For(0, count, parallelOptions, async (i, _) =>
        {
            var index = (uint)i;
#else
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = maxDop };
        await Parallel.ForAsync<uint>(0, count, parallelOptions, async (index, _) =>
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

    private static uint ResolveCount(IDataContext dataContext, ForNodeConfiguration config)
    {
        if (!string.IsNullOrWhiteSpace(config.CountPath))
        {
            var resolved = dataContext.GetSimpleValueByPath<long?>(config.CountPath);
            if (resolved == null)
            {
                throw new InvalidOperationException(
                    $"CountPath '{config.CountPath}' resolved to null. Provide a valid integer value at the specified path.");
            }

            if (resolved < 0)
            {
                throw new InvalidOperationException(
                    $"CountPath '{config.CountPath}' resolved to {resolved}. Count must be a non-negative integer.");
            }

            return (uint)resolved.Value;
        }

        return config.Count;
    }
}