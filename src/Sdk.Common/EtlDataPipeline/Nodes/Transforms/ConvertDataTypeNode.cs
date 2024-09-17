using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;

/// <summary>
/// Configuration data type conversion
/// </summary>
[NodeName("ConvertDataType", 1)]
public class ConvertDataTypeNodeConfiguration : SourceTargetPathNodeConfiguration
{
    /// <summary>
    /// Data type that the value is cast to during transformation
    /// </summary>
    public AttributeValueTypesDto ValueType { get; set; }
}

/// <summary>
/// Convert the value to the defined data type
/// </summary>
[NodeConfiguration(typeof(ConvertDataTypeNodeConfiguration))]
public class ConvertDataTypeNode(NodeDelegate next) : IPipelineNode
{
    /// <inheritdoc />
    public async Task ProcessObjectAsync(IDataContext dataContext)
    {
        var c = dataContext.NodeContext.GetNodeConfiguration<ConvertDataTypeNodeConfiguration>();

        if (dataContext.Current == null)
        {
            dataContext.SetValueByPath<object>(c.TargetPath, c.TargetValueKind, c.TargetValueWriteMode, null);
            return;
        }

        var sourceValue = dataContext.GetSimpleValueByPath<object>(c.Path);
        var value = ConvertPrimitiveValue(c, sourceValue);
        if (sourceValue is JValue jValue)
        {
            dataContext.SetValueByPath(c.TargetPath, c.TargetValueKind, c.TargetValueWriteMode, value);
        }
        else
        {
            throw DataPipelineException.ValueIsObjectButMustBePrimitive(c.Path);
        }

        await next(dataContext);
    }

    private static object? ConvertPrimitiveValue(ConvertDataTypeNodeConfiguration c, object? sourceValue)
    {
        object? value;
        switch (c.ValueType)
        {
            case AttributeValueTypesDto.String:
                value = Convert.ToString(sourceValue);
                break;
            case AttributeValueTypesDto.Int:
                value = Convert.ToInt32(sourceValue);
                break;
            case AttributeValueTypesDto.Int64:
                value = Convert.ToInt64(sourceValue);
                break;
            case AttributeValueTypesDto.Boolean:
                value = Convert.ToBoolean(sourceValue);
                break;
            case AttributeValueTypesDto.Double:
                value = Convert.ToDouble(sourceValue);
                break;
            default:
                throw DataPipelineException.ValueTypeUnsupported(c.Path, c.ValueType);
        }

        return value;
    }
}