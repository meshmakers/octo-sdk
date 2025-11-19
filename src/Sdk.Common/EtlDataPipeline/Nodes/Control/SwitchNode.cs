using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.Services;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;

/// <summary>
/// Represents a case in a switch statement.
/// </summary>
public record SwitchCase
{
    /// <summary>
    /// The value or values to match against. 
    /// Supports single values or collections to enable fall-through/case stacking behavior,
    /// where multiple values execute the same transformations.
    /// </summary>
    public required object Value { get; set; }

    /// <summary>
    /// The transformations to execute if this case matches.
    /// </summary>
    public required ICollection<NodeConfiguration> Transformations { get; set; }
}

/// <summary>
/// Configuration for a switch node.
/// </summary>
[NodeName("Switch", 1)]
public record SwitchNodeConfiguration : PathNodeConfiguration, IChildNodeConfiguration
{
    /// <summary>
    /// The cases to evaluate.
    /// </summary>
    public required ICollection<SwitchCase> Cases { get; set; }

    /// <summary>
    /// Default transformations to execute if no case matches.
    /// </summary>
    public ICollection<NodeConfiguration>? Default { get; set; }

    /// <summary>
    /// Defines the value type for comparison.
    /// </summary>
    public AttributeValueTypesDto ValueType { get; set; } = AttributeValueTypesDto.String;

    /// <inheritdoc />
    public ICollection<NodeConfiguration>? Transformations { get; set; }
}

/// <summary>
/// A node that executes different transformations based on a switch statement.
/// </summary>
[NodeConfiguration(typeof(SwitchNodeConfiguration))]
// ReSharper disable once ClassNeverInstantiated.Global
public class SwitchNode(NodeDelegate next) : ChildNodeBase
{
    /// <inheritdoc />
    public override async Task ProcessObjectAsync(IDataContext dataContext, INodeContext nodeContext)
    {
        var c = nodeContext.GetNodeConfiguration<SwitchNodeConfiguration>();

        var value = GetValueFromDataContext(nodeContext, dataContext, c.Path, c.ValueType);

        // Find a matching case
        SwitchCase? matchingCase = null;
        string? matchDescription = null;
        foreach (var switchCase in c.Cases)
        {
            var (isMatch, description) = IsMatch(value, switchCase.Value, c.ValueType, nodeContext);
            if (isMatch)
            {
                matchingCase = switchCase;
                matchDescription = description;
                break;
            }
        }

        if (matchingCase != null)
        {
            nodeContext.Debug($"Switch case matched: {matchDescription}. Processing case transformations.");
            await ExecuteTransformations(dataContext, nodeContext, matchingCase.Transformations);
            nodeContext.Debug("Case transformations done.");
        }
        else if (c.Default != null)
        {
            nodeContext.Debug("No case matched. Processing default transformations.");
            await ExecuteTransformations(dataContext, nodeContext, c.Default);
            nodeContext.Debug("Default transformations done.");
        }
        else
        {
            nodeContext.Debug("No case matched and no default transformations defined.");
        }

        await next(dataContext, nodeContext);
    }

    private static async Task ExecuteTransformations(IDataContext dataContext, INodeContext nodeContext,
        ICollection<NodeConfiguration> transformations)
    {
        var (itemContext, itemNodeContext) = nodeContext.CreateSubContext(dataContext.Current, 0,
            nodeContext.GetNodeConfiguration<SwitchNodeConfiguration>(), dataContext);

        var last = new NodeDelegate((ds, _) =>
        {
            itemNodeContext.Unregister(ds);
            dataContext.Current = ds.Current;
            return Task.CompletedTask;
        });

        var tempConfiguration = new SwitchNodeConfiguration
        {
            Path = "$",
            Cases = [],
            Transformations = transformations
        };

        await ProcessChildTransformationsAsSequenceAsync(itemContext, nodeContext, last, tempConfiguration);
    }

    private static (bool isMatch, string? description) IsMatch(object? value, object caseValue, AttributeValueTypesDto valueType, INodeContext nodeContext)
    {
        // Check if caseValue is a collection (array or list)
        if (caseValue is System.Collections.IEnumerable enumerable and not string)
        {
            // Check if value matches any of the values in the collection
            var values = new List<object>();
            foreach (var item in enumerable)
            {
                values.Add(item);
                var convertedCaseValue = ConvertCaseValue(item, valueType, nodeContext);
                if (value?.Equals(convertedCaseValue) ?? (value == null && convertedCaseValue == null))
                {
                    var valuesString = string.Join(", ", values);
                    return (true, $"{value} (matched in [{valuesString}])");
                }
            }
            return (false, null);
        }
        else
        {
            // Single value comparison
            var convertedCaseValue = ConvertCaseValue(caseValue, valueType, nodeContext);
            var isMatch = value?.Equals(convertedCaseValue) ?? (value == null && convertedCaseValue == null);
            return (isMatch, isMatch ? $"{value}" : null);
        }
    }

    private static object? ConvertCaseValue(object value, AttributeValueTypesDto valueType, INodeContext nodeContext)
    {
        return valueType switch
        {
            AttributeValueTypesDto.Boolean => Convert.ToBoolean(value),
            AttributeValueTypesDto.Int => Convert.ToInt32(value),
            AttributeValueTypesDto.Int64 => Convert.ToInt64(value),
            AttributeValueTypesDto.Double => Convert.ToDouble(value),
            AttributeValueTypesDto.String => value.ToString(),
            AttributeValueTypesDto.DateTime => Convert.ToDateTime(value),
            AttributeValueTypesDto.Enum => Convert.ToInt32(value),
            _ => throw PipelineExecutionException.DefinedValueTypeNotSupported(nodeContext.NodePath, valueType,
                value)
        };
    }

    private static object? GetValueFromDataContext(INodeContext nodeContext, IDataContext dataContext, string path,
        AttributeValueTypesDto valueType)
    {
        return valueType switch
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
    }
}