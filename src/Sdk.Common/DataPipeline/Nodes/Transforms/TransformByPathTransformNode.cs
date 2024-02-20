using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.Common.DataPipeline.Configuration;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.DataPipeline.Nodes.Transforms;

/// <summary>
/// Configuration for a assign object node.
/// </summary>
public class TransformByPathConfigurationNode : ObjectIteratorConfigurationNode<TransformPathPropertyConfigurationNode>;

/// <summary>
/// Contains transformation information of a property.
/// </summary>
public class TransformPathPropertyConfigurationNode : TokenConfigurationNode
{
    /// <summary>
    /// Data type that the value is casted to during transformation
    /// </summary>
    public AttributeValueTypesDto ValueType { get; set; }
}

/// <summary>
/// Transforms a list of properties from the source.
/// </summary>
[Node("TransformByPath", 1, typeof(TransformByPathConfigurationNode))]
public class TransformByPathTransformNode : ObjectIteratorNode<TransformByPathConfigurationNode,
    TransformPathPropertyConfigurationNode>
{
    /// <inheritdoc />
    public override async Task ProcessObjectAsync(ITransformDataContext dataContext)
    {
        var c = dataContext.GetNodeConfiguration<TransformByPathConfigurationNode>();

        if (dataContext.Source != null)
        {
            var source = JObject.FromObject(dataContext.Source);

            foreach (var tn in c.Transforms)
            {
                var jToken = source.SelectToken(tn.SourcePath);
                await ProcessToken(dataContext, tn, jToken);
            }
        }
    }
}