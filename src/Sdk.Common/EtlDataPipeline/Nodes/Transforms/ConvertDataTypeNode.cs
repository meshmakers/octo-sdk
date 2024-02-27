using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;

/// <summary>
/// Configuration data type conversion
/// </summary>
public class ConvertDataTypeNodeConfiguration : NodeConfiguration
{
    /// <summary>
    /// Gets or sets the source path
    /// </summary>
    public string? SourcePath { get; set; }
    
    /// <summary>
    /// Target property name
    /// </summary>
    public string? TargetPropertyName { get; set; }
    
    /// <summary>
    /// Data type that the value is casted to during transformation
    /// </summary>
    public AttributeValueTypesDto ValueType { get; set; }
}

/// <summary>
/// Convert the value to the defined data type
/// </summary>
[Node("ConvertDataType", 1, typeof(ConvertDataTypeNodeConfiguration))]
public class ConvertDataTypeNode(NodeDelegate next) : IPipelineNode
{
    /// <inheritdoc />
    public async Task ProcessObjectAsync(IDataContext dataContext)
    {
        var c = dataContext.GetNodeConfiguration<ConvertDataTypeNodeConfiguration>();
        dataContext.Logger.LogDebug("Executing {Node} {Description}", nameof(ConvertDataTypeNode), c.Description);

        if (dataContext.Current == null)
        {
            dataContext.SetCurrentValueByPath<object>(c.TargetPropertyName, null);
            return;
        }
            
        var sourceValue = dataContext.Current!.SelectToken(c.SourcePath ?? "$");
        if (sourceValue is JValue jValue)
        {
            var value = ConvertPrimitiveValue(c, jValue);
            dataContext.SetCurrentValueByPath(c.TargetPropertyName, value);
        }
        else if (sourceValue is JArray jArray)
        {
            JArray array = new JArray();
            foreach (var jToken in jArray)
            {
                if (jToken is JValue jValueElement)
                {
                    var value = ConvertPrimitiveValue(c, jValueElement);
                    array.Add(value);
                }
            }
            dataContext.SetCurrentValueByPath(c.TargetPropertyName, array);
        }
        else
        {
            throw DataPipelineException.ValueIsObjectButMustBePrimitive(c.SourcePath ?? "$");
        }

        dataContext.Logger.LogDebug("Executing {Node} {Description} done - executing next", nameof(ConvertDataTypeNode), c.Description);
        await next(dataContext);
    }

    private static object? ConvertPrimitiveValue(ConvertDataTypeNodeConfiguration c, JValue jValue)
    {
        object? value;
        switch (c.ValueType)
        {
            case AttributeValueTypesDto.String:
                value = jValue.Value<string>();
                break;
            case AttributeValueTypesDto.Int:
                value = jValue.Value<int>();
                break;
            case AttributeValueTypesDto.Int64:
                value = jValue.Value<long>();
                break;
            case AttributeValueTypesDto.Boolean:
                value = jValue.Value<bool>();
                break;
            case AttributeValueTypesDto.Double:
                value = jValue.Value<double>();
                break;
            default:
                throw DataPipelineException.ValueTypeUnsupported(c.SourcePath ?? "$", c.ValueType);
        }

        return value;
    }
}