using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.Services;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Extracts;

/// <summary>
/// Node configuration to set a primitive value on the current object
/// </summary>
[NodeName("SetPrimitiveValue", 1)]
// ReSharper disable once ClassNeverInstantiated.Global
public record SetPrimitiveValueNodeConfiguration : TargetPathNodeConfiguration
{
    /// <summary>
    /// The primitive value to set
    /// </summary>
    public required object Value { get; init; } = null!;

    /// <summary>
    /// The type of the primitive value
    /// </summary>
    public required AttributeValueTypesDto ValueType { get; set; } = AttributeValueTypesDto.String;
}

/// <summary>
/// Sets a primitive value on the current object
/// </summary>
/// <param name="next">Next node in the pipeline</param>
[NodeConfiguration(typeof(SetPrimitiveValueNodeConfiguration))]
// ReSharper disable once ClassNeverInstantiated.Global
public class SetPrimitiveValueNode(NodeDelegate next) : IPipelineNode
{
    /// <inheritdoc />
    public Task ProcessObjectAsync(IDataContext dataContext, INodeContext nodeContext)
    {
        var c = nodeContext.GetNodeConfiguration<SetPrimitiveValueNodeConfiguration>();

        dataContext.SetValueByPath(c.TargetPath, c.DocumentMode, c.TargetValueKind, c.TargetValueWriteMode,
            ConvertToConfiguredType(nodeContext, c.Value, c.ValueType));

        return next(dataContext, nodeContext);
    }

    private object? ConvertToConfiguredType(INodeContext nodeContext, object? value, AttributeValueTypesDto type)
    {
        try
        {
            return type switch
            {
                AttributeValueTypesDto.Int => Convert.ChangeType(value, typeof(int)),
                AttributeValueTypesDto.String => Convert.ChangeType(value, typeof(string)),
                AttributeValueTypesDto.Binary => Convert.ChangeType(value, typeof(byte)),
                AttributeValueTypesDto.Boolean => Convert.ChangeType(value, typeof(bool)),
                AttributeValueTypesDto.DateTime => Convert.ChangeType(value, typeof(DateTime)),
                AttributeValueTypesDto.Double => value is string s
                    ? double.Parse(s, System.Globalization.CultureInfo.InvariantCulture)
                    : Convert.ChangeType(value, typeof(double)),
                AttributeValueTypesDto.StringArray => Convert.ChangeType(value, typeof(string[])),
                AttributeValueTypesDto.IntArray => Convert.ChangeType(value, typeof(int[])),
                AttributeValueTypesDto.TimeSpan => Convert.ChangeType(value, typeof(TimeSpan)),
                AttributeValueTypesDto.Int64 => Convert.ChangeType(value, typeof(long)),

                /* Not Mapped
                    AttributeValueTypesDto.BinaryLinked => dataContext.GetSimpleValueByPath<>(path),
                    AttributeValueTypesDto.Record => dataContext.GetSimpleValueByPath<>(path),
                    AttributeValueTypesDto.RecordArray => dataContext.GetSimpleValueByPath<>(path),
                    AttributeValueTypesDto.Enum => dataContext.GetSimpleValueByPath<>(path),
                    AttributeValueTypesDto.DateTimeOffset => dataContext.GetSimpleValueByPath<>(path),
                    AttributeValueTypesDto.GeospatialPoint => dataContext.GetSimpleValueByPath<>(path),
                */

                _ => throw PipelineExecutionException.DefinedValueTypeNotSupported(nodeContext.NodePath, type, value)
            };
        }
        catch
        {
            nodeContext.Error("Failed to convert value {0} to {1}", value ?? "", type);
            throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
}