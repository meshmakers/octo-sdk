using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;

/// <summary>
/// Configuration data type conversion
/// </summary>
public class ConvertDataTypeNodeConfiguration : TransformNodeConfiguration
{
    /// <summary>
    /// Data type that the value is casted to during transformation
    /// </summary>
    public AttributeValueTypesDto ValueType { get; set; }
}

/// <summary>
/// Convert the value to the defined data type
/// </summary>
[Node("ConvertDataType", 1, typeof(ConvertDataTypeNodeConfiguration))]
public class ConvertDataTypeNode : ITransformPipelineNode
{
    /// <inheritdoc />
    public Task ProcessObjectAsync(ITransformDataContext transformDataContext)
    {
        var c = transformDataContext.GetNodeConfiguration<ConvertDataTypeNodeConfiguration>();

        if (transformDataContext.Source == null)
        {
            transformDataContext.SetTargetValueByName<object>(c.TargetPropertyName, null);
            return Task.CompletedTask;
        }
            
        var sourceValue = transformDataContext.Source!.SelectToken(c.SourcePath ?? "$");
        if (sourceValue is JValue jValue)
        {
            var value = ConvertPrimitiveValue(c, jValue);
            transformDataContext.SetTargetValueByName(c.TargetPropertyName, value);
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
            transformDataContext.SetTargetValueByName(c.TargetPropertyName, array);
        }
        else
        {
            throw DataPipelineException.ValueIsObjectButMustBePrimitive(c.SourcePath ?? "$");
        }

        return Task.CompletedTask;
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