using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;

/// <summary>
/// Configuration of node for pivot transformation, converting a JSON object with multiple columns into a list of objects with a single column.
/// </summary>
/// <remarks>
/// This type of transformation is known as a pivot transformation or data restructuring, specifically a conversion from wide format to long format, where the JSON object, currently organized column-wise, is transformed into a list format with each row (based on the timestamp) as a separate object.
/// </remarks>
[NodeName("Map", 1)]
public record MapNodeConfiguration : SourceTargetPathNodeConfiguration
{
    /// <inheritdoc />
    public MapNodeConfiguration()
    {
        TargetValueKind = ValueKinds.Simple;
    }

    /// <summary>
    /// Properties to transform
    /// </summary>
    [PropertyGroup("Paths", 2, "jsonpath")]
    public IEnumerable<string> SelectPaths { get; set; } = new List<string>();
}

/// <summary>
/// Mapping/Pivot transformation, converting a JSON object with multiple columns into a list of objects with a single column.
/// </summary>
[NodeConfiguration(typeof(MapNodeConfiguration))]
// ReSharper disable once ClassNeverInstantiated.Global
public class MapNode(NodeDelegate next) : IPipelineNode
{
    /// <inheritdoc />
    public async Task ProcessObjectAsync(IDataContext dataContext, INodeContext nodeContext)
    {
        var c = nodeContext.GetNodeConfiguration<MapNodeConfiguration>();

        List<JObject> transformedData = new List<JObject>();
        var data = dataContext.GetSimpleValueByPath<JToken>(c.Path);
        if (data != null)
        {
            // Find the maximum count of all the paths
            int count = 0;
            Dictionary<string, JArray> pathData = new Dictionary<string, JArray>();
            foreach (var path in c.SelectPaths)
            {
                var selectToken = data.SelectToken(path);
                
                if (selectToken is JArray array)
                {
                    count = Math.Max(count, array.Count);

                    pathData[path] = array;
                }
                else
                {
                    nodeContext.Warning($"Path '{path}' is not an array and will be skipped.");
                }
            }

            for (int i = 0; i < count; i++)
            {
                var entry = new JObject();

                foreach (var keyValue in pathData)
                {
                    entry.ReplaceNested(keyValue.Key, keyValue.Value[i]);
                }

                transformedData.Add(entry);
            }
        }

        var target = new JArray { transformedData };
        dataContext.SetValueByPath(c.TargetPath, c.DocumentMode, c.TargetValueKind, c.TargetValueWriteMode, target);

        await next(dataContext, nodeContext);
    }
}