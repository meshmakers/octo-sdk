using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.Services;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;

/// <summary>
/// Defines the comparison operator for an if node.
/// </summary>
public enum CompareOperator
{
    /// <summary>
    /// Equal operator.
    /// </summary>
    Equal = Equals,

    /// <summary>
    /// Equal operator.
    /// </summary>
    Equals = 0,

    /// <summary>
    /// Not equal operator.
    /// </summary>
    NotEqual = NotEquals,

    /// <summary>
    /// Equal operator.
    /// </summary>
    NotEquals = 1,

    /// <summary>
    /// Contains operator if the value is a string.
    /// </summary>
    Contain = 2,

    /// <summary>
    /// Contains operator if the value is a string.
    /// </summary>
    Contains = Contain,

    /// <summary>
    /// Less than operator.
    /// </summary>
    LessThan = 3,

    /// <summary>
    /// Less or equal than operator.
    /// </summary>
    LessEqualsThan = 4,

    /// <summary>
    /// Greater than operator.
    /// </summary>
    GreaterThan = 5,

    /// <summary>
    /// Greater or equal than operator.
    /// </summary>
    GreaterEqualsThan = 6,

    /// <summary>
    /// Starts with operator if the value is a string.
    /// </summary>
    StartsWith = 7,

    /// <summary>
    /// Ends with operator if the value is a string.
    /// </summary>
    EndsWith = 8,

    /// <summary>
    /// Regex match operator if the value is a string.
    /// </summary>
    RegexMatch = 9
}

/// <summary>
/// Configuration for an if node.
/// </summary>
[NodeName("If", 1)]
public record IfNodeConfiguration : PathNodeConfiguration, IChildNodeConfiguration
{
    /// <inheritdoc />
    public required ICollection<NodeConfiguration>? Transformations { get; set; }

    /// <summary>
    /// Defines the comparison operator.
    /// </summary>
    public CompareOperator Operator { get; set; } = CompareOperator.Equal;

    /// <summary>
    /// The path to the value to compare. Either this or <see cref="Value"/> must be set.
    /// </summary>
    public string? ValuePath { get; set; }

    /// <summary>
    /// The value to compare. Either this or <see cref="ValuePath"/> must be set.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Defines the value type
    /// </summary>
    public AttributeValueTypesDto ValueType { get; set; }
}

/// <summary>
/// A node that executes a set of transformations if a condition is met.
/// </summary>
[NodeConfiguration(typeof(IfNodeConfiguration))]
// ReSharper disable once ClassNeverInstantiated.Global
public class IfNode(NodeDelegate next) : ChildNodeBase
{
    /// <inheritdoc />
    public override async Task ProcessObjectAsync(IDataContext dataContext, INodeContext nodeContext)
    {
        var c = nodeContext.GetNodeConfiguration<IfNodeConfiguration>();

        // We support equal with null values!
        var comparisonValue = GetComparisonValue(nodeContext, dataContext, c);
        var value = GetValueFromDataContext(nodeContext, dataContext, c.Path, c.ValueType);

        switch (c.Operator)
        {
            case CompareOperator.Equal:
                if ((value?.Equals(comparisonValue) ?? false) || value == null && comparisonValue == null)
                {
                    await IterateElement(dataContext, nodeContext, c);
                }

                break;
            case CompareOperator.NotEqual:
                if (!((value?.Equals(comparisonValue) ?? false) || value == null && comparisonValue == null))
                {
                    await IterateElement(dataContext, nodeContext, c);
                }

                break;
            case CompareOperator.Contains:
                if (comparisonValue != null && value != null &&
                    (value.ToString()?.ToLower().Contains(comparisonValue.ToString()?.ToLower() ?? "") ?? false))
                {
                    await IterateElement(dataContext, nodeContext, c);
                }

                break;

            case CompareOperator.GreaterThan:
                if (value is IComparable comparableValue1 && comparableValue1.CompareTo(comparisonValue) > 0)
                {
                    await IterateElement(dataContext, nodeContext, c);
                }

                break;

            case CompareOperator.GreaterEqualsThan:
                if (value is IComparable comparableValue2 && comparableValue2.CompareTo(comparisonValue) >= 0)
                {
                    await IterateElement(dataContext, nodeContext, c);
                }

                break;

            case CompareOperator.LessThan:
                if (value is IComparable comparableValue3 && comparableValue3.CompareTo(comparisonValue) < 0)
                {
                    await IterateElement(dataContext, nodeContext, c);
                }

                break;

            case CompareOperator.LessEqualsThan:
                if (value is IComparable comparableValue4 && comparableValue4.CompareTo(comparisonValue) <= 0)
                {
                    await IterateElement(dataContext, nodeContext, c);
                }

                break;

            case CompareOperator.StartsWith:
                if (comparisonValue != null && value != null &&
                    (value.ToString()?.ToLower().StartsWith(comparisonValue.ToString()?.ToLower() ?? "") ?? false))
                {
                    await IterateElement(dataContext, nodeContext, c);
                }

                break;

            case CompareOperator.EndsWith:
                if (comparisonValue != null && value != null &&
                    (value.ToString()?.ToLower().EndsWith(comparisonValue.ToString()?.ToLower() ?? "") ?? false))
                {
                    await IterateElement(dataContext, nodeContext, c);
                }

                break;

            case CompareOperator.RegexMatch:
                if (comparisonValue != null && value != null &&
                    System.Text.RegularExpressions.Regex.IsMatch(value.ToString() ?? "", comparisonValue.ToString() ?? ""))
                {
                    await IterateElement(dataContext, nodeContext, c);
                }

                break;

            default:
                nodeContext.Error($"Operator '{c.Operator}' is not supported.");
                throw PipelineConfigurationException.InvalidOperation($"Operator '{c.Operator}' is not supported.");
        }


        await next(dataContext, nodeContext);
    }

    private static async Task IterateElement(IDataContext dataContext, INodeContext nodeContext, IfNodeConfiguration c)
    {
        var (itemContext, itemNodeContext) =
            nodeContext.CreateSubContext(dataContext.Current, 0, c, dataContext);

        var last = new NodeDelegate((ds, _) =>
        {
            itemNodeContext.Unregister(ds);
            dataContext.Current = ds.Current;
            return Task.CompletedTask;
        });

        nodeContext.Debug("Condition met. Processing child transformations.");
        await ProcessChildTransformationsAsSequenceAsync(itemContext, nodeContext, last, c);
        nodeContext.Debug("Child transformations done.");
    }

    private object? GetComparisonValue(INodeContext nodeContext, IDataContext dataContext, IfNodeConfiguration c)
    {
        if (c.Value != null)
        {
            return c.ValueType switch
            {
                AttributeValueTypesDto.Boolean => Convert.ToBoolean(c.Value),
                AttributeValueTypesDto.Int => Convert.ToInt32(c.Value),
                AttributeValueTypesDto.Int64 => Convert.ToInt64(c.Value),
                AttributeValueTypesDto.Double => Convert.ToDouble(c.Value),
                AttributeValueTypesDto.String => (string)c.Value,
                AttributeValueTypesDto.DateTime => Convert.ToDateTime(c.Value),
                AttributeValueTypesDto.Enum => Convert.ToInt32(c.Value),
                _ => throw PipelineExecutionException.DefinedValueTypeNotSupported(nodeContext.NodePath, c.ValueType, c.Value)
            };
        }

        if (c.ValuePath != null)
        {
           return GetValueFromDataContext(nodeContext, dataContext, c.ValuePath, c.ValueType);
        }
        
        // if the value is null, it CAN be a valid case, we just want to make sure that some value is defined
        return null;
    }

    private static object? GetValueFromDataContext(INodeContext nodeContext, IDataContext dataContext, string path,
        AttributeValueTypesDto valueType)
    {
        object? value = valueType switch
        {
            AttributeValueTypesDto.Boolean => dataContext.GetSimpleValueByPath<bool>(path),
            AttributeValueTypesDto.Int => dataContext.GetSimpleValueByPath<int>(path),
            AttributeValueTypesDto.Int64 => dataContext.GetSimpleValueByPath<long>(path),
            AttributeValueTypesDto.Double => dataContext.GetSimpleValueByPath<double>(path),
            AttributeValueTypesDto.String => dataContext.GetSimpleValueByPath<string>(path),
            AttributeValueTypesDto.DateTime => dataContext.GetSimpleValueByPath<DateTime>(path),
            AttributeValueTypesDto.Enum => dataContext.GetSimpleValueByPath<int>(path),
            _ => throw PipelineExecutionException.ValueTypeNotSupported(nodeContext.NodePath, valueType, path)
        };
        return value;
    }
}