using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;

/// <summary>
/// Defines the comparison operator for an if node.
/// </summary>
public enum CompareOperator
{
    /// <summary>
    /// Equal operator.
    /// </summary>
    Equal = 0,
    
    /// <summary>
    /// Not equal operator.
    /// </summary>
    NotEqual = 1,
    
    /// <summary>
    /// Contains operator if the value is a string.
    /// </summary>
    Contains = 2,
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
    /// <summary>
    /// processing of the node
    /// </summary>
    /// <param name="dataContext"></param>
    /// <returns></returns>
    public override async Task ProcessObjectAsync(IDataContext dataContext)
    {
        var c = dataContext.NodeContext.GetNodeConfiguration<IfNodeConfiguration>();

        var comparisonValue = GetComparisonValue(dataContext, c);
        if (comparisonValue == null)
        {
            return;
        }

        var value = GetValueFromDataContext(dataContext, c.Path, c.ValueType);

        switch (c.Operator)
        {
            case CompareOperator.Equal:
                if (value != null && value.Equals(comparisonValue))
                {
                    await IterateElement(dataContext, c);
                }

                break;
            case CompareOperator.NotEqual:
                if (value != null && !value.Equals(comparisonValue))
                {
                    await IterateElement(dataContext, c);
                }

                break;
            case CompareOperator.Contains:
                if (value != null && (value.ToString()?.ToLower().Contains(comparisonValue.ToString()?.ToLower() ?? "") ?? false))
                {
                    await IterateElement(dataContext, c);
                }

                break;
                
            default:
                dataContext.NodeContext.Error($"Operator '{c.Operator}' is not supported.");
                throw PipelineConfigurationException.InvalidOperation($"Operator '{c.Operator}' is not supported.");
        }


        await next(dataContext);
    }

    private static async Task IterateElement(IDataContext dataContext, IfNodeConfiguration c)
    {
        var (itemContext, itemNodeContext) = dataContext.CreateSubContext(dataContext.Current,  dataContext.NodeContext,"", 0, c);
            
        var last = new NodeDelegate(d =>
        {
            itemNodeContext.Complete(d);
            return Task.CompletedTask;
        });

        dataContext.NodeContext.Debug("Condition met. Processing child transformations.");
        await ProcessChildTransformationsAsSequenceAsync(itemContext, last, c);
        dataContext.NodeContext.Debug("Child transformations done.");
    }

    private object? GetComparisonValue(IDataContext dataContext, IfNodeConfiguration c)
    {
        if (c.Value == null && c.ValuePath == null)
        {
            dataContext.NodeContext.Error("Either Value or ValuePath must be set.");
            return null;
        }

        if (c.Value != null)
        {
            return c.ValueType switch
            {
                AttributeValueTypesDto.Boolean => Convert.ToBoolean(c.Value),
                AttributeValueTypesDto.Int => Convert.ToInt32(c.Value),
                AttributeValueTypesDto.Int64 => Convert.ToInt64(c.Value),
                AttributeValueTypesDto.Double => Convert.ToDouble(c.Value),
                AttributeValueTypesDto.String => (string) c.Value,
                AttributeValueTypesDto.DateTime => Convert.ToDateTime(c.Value),
                _ => null
            };
        }

        var value = GetValueFromDataContext(dataContext, c.Path, c.ValueType);

        if (value == null)
        {
            dataContext.NodeContext.Error($"Value type '{c.ValueType}' is not supported.", c.ValueType);
        }

        return value;
    }

    private static object? GetValueFromDataContext(IDataContext dataContext, string path, AttributeValueTypesDto valueType)
    {
        object? value = valueType switch
        {
            AttributeValueTypesDto.Boolean => dataContext.GetSimpleValueByPath<bool>(path),
            AttributeValueTypesDto.Int => dataContext.GetSimpleValueByPath<int>(path),
            AttributeValueTypesDto.Int64 => dataContext.GetSimpleValueByPath<long>(path),
            AttributeValueTypesDto.Double => dataContext.GetSimpleValueByPath<double>(path),
            AttributeValueTypesDto.String => dataContext.GetSimpleValueByPath<string>(path),
            AttributeValueTypesDto.DateTime => dataContext.GetSimpleValueByPath<DateTime>(path),
            _ => null
        };
        return value;
    }
}