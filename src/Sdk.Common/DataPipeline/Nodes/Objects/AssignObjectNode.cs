using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.Common.DataPipeline.Configuration;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.DataPipeline.Nodes.Objects;


/// <summary>
/// Configuration for a assign object node.
/// </summary>
[Node("AssignObject", 1, typeof(AssignObjectTransformationNode))]
public class AssignObjectConfigurationNode : ConfigurationNode
{
    /// <summary>
    /// List of transformations
    /// </summary>
    public ICollection<AssignObjectTransformationNode> TransformList { get; set; } = null!;
}

/// <summary>
/// Contains transformation information of a property.
/// </summary>
public class AssignObjectTransformationNode
{
    /// <summary>
    /// Path to extract using JSON Paths a value 
    /// </summary>
    public string Path { get; set; } = null!;
    
    /// <summary>
    /// Name of property that is used internal
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Data type that the value is casted to during transformation
    /// </summary>
    public AttributeValueTypesDto ValueType { get; set; }
}

/// <summary>
/// Collects data from a source object
/// </summary>
public class AssignObjectNode : IObjectPipelineNode
{
    /// <inheritdoc />
    public Task<object?> ProcessObjectAsync(IObjectDataContext dataContext)
    {
        var c = dataContext.GetNodeConfiguration<AssignObjectConfigurationNode>();

        var source = JObject.FromObject(dataContext.Source);
        RtEntityDto rtEntityDto = new();
        foreach (var tn in c.TransformList)
        {
            var v = source.SelectToken(tn.Path);

            rtEntityDto.Properties ??= new Dictionary<string, object?>();

            switch (tn.ValueType)
            {
                case AttributeValueTypesDto.String:
                    rtEntityDto.Properties[tn.Name] = v?.Value<string>();
                    break;
                case AttributeValueTypesDto.Int:
                    rtEntityDto.Properties[tn.Name] = v?.Value<int>();
                    break;
                case AttributeValueTypesDto.Int64:
                    rtEntityDto.Properties[tn.Name] = v?.Value<long>();
                    break;
                case AttributeValueTypesDto.Boolean:
                    rtEntityDto.Properties[tn.Name] = v?.Value<bool>();
                    break;
                case AttributeValueTypesDto.Double:
                    rtEntityDto.Properties[tn.Name] = v?.Value<double>();
                    break;
                default:
                    throw DataPipelineException.ValueTypeUnsupported(tn.Name, tn.ValueType);
            }
        }

        return Task.FromResult((object?)rtEntityDto);
    }
}