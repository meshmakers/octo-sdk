using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;

/// <summary>
/// Configuration for removing duplicates from an array.
/// Supports scalar arrays (strings, numbers, etc.) and object arrays (deduplicated by a property).
/// </summary>
[NodeName("Distinct", 1)]
public record DistinctNodeConfiguration : SourceTargetPathNodeConfiguration
{
    /// <summary>
    /// Optional path to the value that defines uniqueness within object arrays.
    /// When not set, the node deduplicates scalar values directly.
    /// When set, each array item must be an object with a property at this path
    /// of a simple type (int, double, string, bool, datetime).
    /// </summary>
    [PropertyGroup("Query", 0, "jsonpath")]
    public string? DistinctValuePath { get; set; }
}

/// <summary>
/// Removes duplicates from an array. Supports both scalar arrays and object arrays.
/// </summary>
[NodeConfiguration(typeof(DistinctNodeConfiguration))]
// ReSharper disable once ClassNeverInstantiated.Global
public class DistinctNode(NodeDelegate next) : IPipelineNode
{
    /// <inheritdoc />
    public async Task ProcessObjectAsync(IDataContext dataContext, INodeContext nodeContext)
    {
        var c = nodeContext.GetNodeConfiguration<DistinctNodeConfiguration>();

        if (dataContext.Current == null)
        {
            await next(dataContext, nodeContext);
            return;
        }

        var sourceToken = dataContext.Current.SelectToken(c.Path);
        if (sourceToken is not JArray sourceArray || sourceArray.Count == 0)
        {
            await next(dataContext, nodeContext);
            return;
        }

        var seenValues = new HashSet<object>();
        var distinctItems = new List<JToken>();

        foreach (var item in sourceArray)
        {
            JToken? uniqueToken;
            if (string.IsNullOrWhiteSpace(c.DistinctValuePath))
            {
                // Scalar array — use the item itself as uniqueness key
                uniqueToken = item is JValue ? item : null;
            }
            else if (item is JObject jObject)
            {
                // Object array — deduplicate by property at DistinctValuePath
                uniqueToken = jObject.SelectToken(c.DistinctValuePath!);
            }
            else
            {
                continue;
            }

            if (uniqueToken == null)
            {
                continue;
            }

            var uniqueValue = ConvertTokenToValue(uniqueToken);
            if (uniqueValue != null && seenValues.Add(uniqueValue))
            {
                distinctItems.Add(item);
            }
        }

        dataContext.SetValueByPath(c.TargetPath, c.DocumentMode, c.TargetValueKind, c.TargetValueWriteMode,
            distinctItems);

        await next(dataContext, nodeContext);
    }

    private static object? ConvertTokenToValue(JToken token) => token.Type switch
    {
        JTokenType.Integer => token.Value<long>(),
        JTokenType.Float => token.Value<double>(),
        JTokenType.String => token.Value<string>(),
        JTokenType.Boolean => token.Value<bool>(),
        JTokenType.Date => token.Value<DateTime>(),
        _ => token.ToString()
    };
}
