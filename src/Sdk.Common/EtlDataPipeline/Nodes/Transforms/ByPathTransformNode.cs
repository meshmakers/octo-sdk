using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;

/// <summary>
/// Configuration for a assign object node.
/// </summary>
public class ByPathNodeConfiguration : ObjectIteratorNodeConfiguration<PathPropertyConfigurationNode>;

/// <summary>
/// Contains transformation information of a property.
/// </summary>
public class PathPropertyConfigurationNode : TokenConfigurationNode;

/// <summary>
/// Transforms a list of properties from the source.
/// </summary>
[Node("TransformByPath", 1, typeof(ByPathNodeConfiguration))]
public class ByPathTransformNode : ObjectIteratorNode<PathPropertyConfigurationNode>
{
    /// <inheritdoc />
    public override async Task ProcessObjectAsync(ITransformDataContext dataContext)
    {
        var c = dataContext.GetNodeConfiguration<ByPathNodeConfiguration>();

        if (dataContext.Source != null)
        {
            var source = JObject.FromObject(dataContext.Source);

            foreach (var tn in c.Transformations)
            {
                var jToken = source.SelectToken(tn.SourcePath ?? "$");
                await ProcessToken(dataContext, tn, jToken);
            }
        }
    }
}