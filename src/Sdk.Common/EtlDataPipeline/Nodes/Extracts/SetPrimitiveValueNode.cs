using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.Services;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Extracts;

/// <summary>
/// Configuration for a node that sets primitive values at specified paths in the data context.
/// </summary>
/// <remarks>
/// <para>
/// The SetPrimitiveValueNode provides the capability to inject static primitive values into the data pipeline
/// at any specified JSONPath location. This node is essential for data enrichment, default value assignment,
/// constants injection, and data structure initialization within ETL processes.
/// </para>
/// <para>
/// Key capabilities include:
/// - Setting primitive values (strings, numbers, booleans, dates, arrays) at any JSONPath location
/// - Automatic type conversion from the source value to the target primitive type
/// - Support for all major primitive data types and their array equivalents
/// - Culture-invariant parsing for numeric values to ensure consistency across different locales
/// - Integration with the target path configuration system for flexible value placement
/// </para>
/// <para>
/// This node is particularly useful for:
/// - Adding metadata timestamps or processing markers
/// - Setting default values for optional fields
/// - Injecting configuration constants into data records
/// - Initializing computed fields with base values
/// - Adding version numbers or processing identifiers
/// </para>
/// </remarks>
/// <example>
/// Configuration to add a processing timestamp:
/// <code>
/// {
///   "TargetPath": "$.metadata.processedAt",
///   "Value": "2023-10-15T14:30:00Z",
///   "ValueType": "DateTime",
///   "DocumentMode": "Extend",
///   "TargetValueKind": "Simple",
///   "TargetValueWriteMode": "Overwrite"
/// }
/// </code>
/// </example>
[NodeName("SetPrimitiveValue", 1)]
// ReSharper disable once ClassNeverInstantiated.Global
public record SetPrimitiveValueNodeConfiguration : TargetPathNodeConfiguration
{
    /// <summary>
    /// Gets or sets the primitive value that will be converted and assigned to the target path.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This value undergoes automatic type conversion based on the <see cref="ValueType"/> property.
    /// The conversion process supports standard .NET type conversions plus specialized handling
    /// for culture-invariant numeric parsing.
    /// </para>
    /// <para>
    /// The value can be provided in various formats:
    /// - Direct primitive values (strings, numbers, booleans)
    /// - String representations that can be parsed to the target type
    /// - Array values for array target types
    /// - DateTime strings in ISO 8601 format for DateTime types
    /// </para>
    /// <para>
    /// For numeric types (Double), string values are parsed using invariant culture to ensure
    /// consistent behavior regardless of the system's regional settings.
    /// </para>
    /// </remarks>
    /// <example>
    /// Examples of valid values for different target types:
    /// <list type="bullet">
    /// <item><description>"42" or 42 for Int type</description></item>
    /// <item><description>"3.14159" or 3.14159 for Double type</description></item>
    /// <item><description>"true" or true for Boolean type</description></item>
    /// <item><description>"2023-12-25T00:00:00Z" for DateTime type</description></item>
    /// <item><description>["item1", "item2"] for StringArray type</description></item>
    /// </list>
    /// </example>
    /// <value>The source value to be converted and assigned to the target path.</value>
    public required object Value { get; init; } = null!;

    /// <summary>
    /// Gets or sets the target primitive type that the <see cref="Value"/> will be converted to before assignment.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property determines how the <see cref="Value"/> will be interpreted and converted during processing.
    /// The conversion process uses .NET's standard type conversion mechanisms with specialized handling
    /// for certain types to ensure reliable and predictable behavior.
    /// </para>
    /// <para>
    /// Supported primitive types include:
    /// - <see cref="AttributeValueTypesDto.String"/>: Text values
    /// - <see cref="AttributeValueTypesDto.Int"/>: 32-bit integers
    /// - <see cref="AttributeValueTypesDto.Int64"/>: 64-bit integers (long)
    /// - <see cref="AttributeValueTypesDto.Double"/>: Double-precision floating-point numbers (culture-invariant parsing)
    /// - <see cref="AttributeValueTypesDto.Boolean"/>: Boolean true/false values
    /// - <see cref="AttributeValueTypesDto.DateTime"/>: Date and time values
    /// - <see cref="AttributeValueTypesDto.TimeSpan"/>: Time duration values
    /// - <see cref="AttributeValueTypesDto.Binary"/>: Byte values
    /// - <see cref="AttributeValueTypesDto.StringArray"/>: Arrays of strings
    /// - <see cref="AttributeValueTypesDto.IntArray"/>: Arrays of integers
    /// </para>
    /// <para>
    /// Note: Complex types (Record, RecordArray, Enum, DateTimeOffset, GeospatialPoint, BinaryLinked)
    /// are not supported by this node and will result in a <see cref="PipelineExecutionException"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// Type selection examples:
    /// <list type="bullet">
    /// <item><description>Use <see cref="AttributeValueTypesDto.String"/> for text content, IDs, or labels</description></item>
    /// <item><description>Use <see cref="AttributeValueTypesDto.Int"/> for counters, quantities, or identifiers</description></item>
    /// <item><description>Use <see cref="AttributeValueTypesDto.Double"/> for monetary amounts, measurements, or calculations</description></item>
    /// <item><description>Use <see cref="AttributeValueTypesDto.Boolean"/> for flags, switches, or binary states</description></item>
    /// <item><description>Use <see cref="AttributeValueTypesDto.DateTime"/> for timestamps, dates, or scheduling information</description></item>
    /// </list>
    /// </example>
    /// <value>The target primitive type for value conversion. Defaults to <see cref="AttributeValueTypesDto.String"/>.</value>
    public required AttributeValueTypesDto ValueType { get; set; } = AttributeValueTypesDto.String;
}

/// <summary>
/// A data extraction node that sets primitive values at specified paths within the data context.
/// </summary>
/// <remarks>
/// <para>
/// The SetPrimitiveValueNode performs value injection operations by converting a configured value
/// to a specified primitive type and assigning it to a target path in the data context. This node
/// is fundamental for data enrichment, default value assignment, and metadata injection in ETL pipelines.
/// </para>
/// <para>
/// Processing workflow:
/// 1. Retrieve the configured value and target type from the node configuration
/// 2. Convert the value to the specified primitive type using culture-invariant parsing where applicable
/// 3. Set the converted value at the target path using the inherited target path configuration
/// 4. Continue to the next node in the pipeline
/// </para>
/// <para>
/// The node supports robust type conversion with specialized handling for numeric types to ensure
/// consistent behavior across different system locales. For Double values, string inputs are parsed
/// using invariant culture formatting to prevent regional formatting issues.
/// </para>
/// <para>
/// Error handling includes detailed logging and specific exceptions for unsupported value types
/// or conversion failures, making debugging and troubleshooting straightforward.
/// </para>
/// </remarks>
/// <example>
/// Example usage for setting a default status:
/// <code>
/// // Configuration:
/// {
///   "TargetPath": "$.status",
///   "Value": "pending",
///   "ValueType": "String",
///   "DocumentMode": "Extend",
///   "TargetValueKind": "Simple",
///   "TargetValueWriteMode": "OverwriteIfNotExists"
/// }
///
/// // Input data:
/// {
///   "orderId": "12345",
///   "customerName": "John Doe"
/// }
///
/// // Output data:
/// {
///   "orderId": "12345",
///   "customerName": "John Doe",
///   "status": "pending"
/// }
/// </code>
/// </example>
/// <example>
/// Example usage for setting a numeric calculation result:
/// <code>
/// // Configuration:
/// {
///   "TargetPath": "$.pricing.taxRate",
///   "Value": "0.0875",
///   "ValueType": "Double",
///   "DocumentMode": "Extend",
///   "TargetValueKind": "Simple",
///   "TargetValueWriteMode": "Overwrite"
/// }
///
/// // Sets a tax rate of 8.75% as a double value
/// </code>
/// </example>
/// <param name="next">The next node in the pipeline to execute after this value assignment completes.</param>
[NodeConfiguration(typeof(SetPrimitiveValueNodeConfiguration))]
// ReSharper disable once ClassNeverInstantiated.Global
public class SetPrimitiveValueNode(NodeDelegate next) : IPipelineNode
{
    /// <summary>
    /// Processes the data context by converting and setting the configured primitive value at the target path.
    /// </summary>
    /// <param name="dataContext">The data context containing the JSON data to modify.</param>
    /// <param name="nodeContext">The node context containing configuration and logging capabilities.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="PipelineExecutionException">
    /// Thrown when the specified <see cref="SetPrimitiveValueNodeConfiguration.ValueType"/> is not supported.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when value conversion fails due to incompatible types or invalid format.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method performs the core value assignment operation by:
    /// 1. Retrieving the configuration including target path, value, and value type
    /// 2. Converting the configured value to the specified primitive type
    /// 3. Using the inherited target path configuration to set the value in the data context
    /// 4. Proceeding to the next node in the pipeline,
    /// </para>
    /// <para>
    /// The value conversion process handles various input formats and performs culture-invariant
    /// parsing for numeric types to ensure consistent behavior. If conversion fails, detailed
    /// error information is logged before throwing an exception.
    /// </para>
    /// <para>
    /// The target path assignment respects all inherited configuration options including
    /// document mode, target value kind, and write mode, providing flexible integration
    /// with existing data structures.
    /// </para>
    /// </remarks>
    public Task ProcessObjectAsync(IDataContext dataContext, INodeContext nodeContext)
    {
        var c = nodeContext.GetNodeConfiguration<SetPrimitiveValueNodeConfiguration>();

        dataContext.SetValueByPath(c.TargetPath, c.DocumentMode, c.TargetValueKind, c.TargetValueWriteMode,
            ConvertToConfiguredType(nodeContext, c.Value, c.ValueType));

        return next(dataContext, nodeContext);
    }

    /// <summary>
    /// Converts the input value to the specified primitive type using appropriate conversion strategies.
    /// </summary>
    /// <param name="nodeContext">The node context for error reporting and logging.</param>
    /// <param name="value">The source value to convert.</param>
    /// <param name="type">The target primitive type for conversion.</param>
    /// <returns>The converted value as the specified primitive type.</returns>
    /// <exception cref="PipelineExecutionException">
    /// Thrown when the specified value type is not supported by this node.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the conversion process fails due to incompatible types or invalid format.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method implements type-specific conversion logic with the following strategies:
    /// - Standard .NET type conversion for most primitive types
    /// - Culture-invariant parsing for Double values to ensure consistent numeric interpretation
    /// - Comprehensive error handling with detailed logging for troubleshooting
    /// </para>
    /// <para>
    /// Supported conversions include:
    /// - Numeric types (Int, Int64, Double, Binary) with automatic parsing from strings
    /// - String conversions with null handling
    /// - Boolean conversions supporting various string representations
    /// - DateTime parsing supporting multiple formats
    /// - TimeSpan conversions for duration values
    /// - Array conversions for StringArray and IntArray types
    /// </para>
    /// <para>
    /// The method specifically handles Double conversions with invariant culture parsing
    /// when the input is a string, preventing regional formatting issues that could cause
    /// parsing failures or incorrect values.
    /// </para>
    /// </remarks>
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