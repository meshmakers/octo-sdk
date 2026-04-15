using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.Services;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms.Aggregations;

/// <summary>
/// Configuration for a sum aggregation node that calculates weighted sums from multiple data sources with optional filtering.
/// </summary>
/// <remarks>
/// <para>
/// The sum aggregation node performs complex mathematical aggregations by:
/// 1. Selecting data from multiple paths using JSONPath expressions
/// 2. Optionally filtering the selected data based on comparison criteria
/// 3. Extracting numeric values from the filtered data
/// 4. Applying multipliers (weights) to each value before summing
/// 5. Storing the final aggregated result at the specified target path
/// </para>
/// <para>
/// This node is particularly useful for financial calculations, inventory summations,
/// statistical aggregations, and any scenario requiring weighted sums across heterogeneous data sources.
/// </para>
/// </remarks>
/// <example>
/// Configuration for calculating total order value with tax:
/// <code>
/// {
///   "TargetPath": "$.totalOrderValue",
///   "Aggregations": [
///     {
///       "Path": "$.orderItems[*]",
///       "FilterPath": null,
///       "ComparisonValue": null,
///       "AggregationPath": "$.price",
///       "Value": 1.0
///     },
///     {
///       "Path": "$.taxes[*]",
///       "FilterPath": "$.type",
///       "ComparisonValue": "VAT",
///       "AggregationPath": "$.amount",
///       "Value": 1.0
///     }
///   ]
/// }
/// </code>
/// This calculates the sum of all item prices plus VAT taxes.
/// </example>
[NodeName("SumAggregation", 1)]
public record SumAggregationNodeConfiguration : TargetPathNodeConfiguration
{
    /// <summary>
    /// Gets or sets the collection of aggregation items that define the data sources, filters, and weights for the sum calculation.
    /// </summary>
    /// <remarks>
    /// Each aggregation item represents a separate data source that contributes to the final sum.
    /// The aggregation items are processed sequentially, and their weighted values are accumulated
    /// to produce the final result. The order of items does not affect the mathematical result
    /// but may impact performance if some items have more restrictive filters.
    /// </remarks>
    /// <value>
    /// A collection of <see cref="SumAggregationItem"/> objects, each defining a source path,
    /// optional filter criteria, aggregation path, and multiplier value.
    /// </value>
    [PropertyGroup("Data Mapping", 0)]
    public required IEnumerable<SumAggregationItem> Aggregations { get; init; }
}

/// <summary>
/// Represents a single data source configuration for sum aggregation, defining how to select, filter, and weight values for aggregation.
/// </summary>
/// <remarks>
/// <para>
/// Each SumAggregationItem defines a complete data pipeline for one contribution to the final sum:
/// 1. <see cref="Path"/> selects the container objects from the root data
/// 2. <see cref="FilterPath"/> and <see cref="ComparisonValue"/> optionally filter these objects
/// 3. <see cref="AggregationPath"/> extracts numeric values from the filtered objects
/// 4. <see cref="Value"/> multiplies each extracted value before adding to the sum
/// </para>
/// <para>
/// The filtering mechanism performs exact string comparison. All selected tokens at the FilterPath
/// must match the ComparisonValue for the object to be included in the aggregation.
/// </para>
/// </remarks>
/// <example>
/// Example item configuration for summing discounted prices:
/// <code>
/// {
///   "Path": "$.orderItems[*]",
///   "FilterPath": "$.status",
///   "ComparisonValue": "active",
///   "AggregationPath": "$.price",
///   "Value": 0.9
/// }
/// </code>
/// This sums prices of active items with a 10% discount (0.9 multiplier).
/// </example>
// ReSharper disable once ClassNeverInstantiated.Global
public record SumAggregationItem
{
    /// <summary>
    /// Gets or sets the JSONPath expression that selects the container objects to process from the root data context.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This path is evaluated against the root data context and should select an array of objects
    /// or individual objects that will be further processed. Each selected object becomes a candidate
    /// for filtering and value extraction.
    /// </para>
    /// <para>
    /// Use array selectors like "[*]" to process multiple objects, or specific indices like "[0]"
    /// to target individual objects.
    /// </para>
    /// </remarks>
    /// <example>
    /// Examples of valid paths:
    /// <list type="bullet">
    /// <item><description>"$.items[*]" - selects all items in the items array</description></item>
    /// <item><description>"$.orders[?(@.status=='pending')]" - selects pending orders (if supported by JSONPath implementation)</description></item>
    /// <item><description>"$.data.products[*]" - selects all products in nested data structure</description></item>
    /// </list>
    /// </example>
    /// <value>A JSONPath expression string that must select one or more objects.</value>
    public required string Path { get; init; }

    /// <summary>
    /// Gets or sets the optional JSONPath expression used to filter the objects selected by <see cref="Path"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When specified, this path is evaluated relative to each object selected by <see cref="Path"/>.
    /// The values found at this path are compared with <see cref="ComparisonValue"/> using exact string matching.
    /// Only objects where ALL tokens at the FilterPath match the ComparisonValue are included in the aggregation.
    /// </para>
    /// <para>
    /// Set to null or empty to disable filtering and include all objects selected by <see cref="Path"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// Filter examples:
    /// <list type="bullet">
    /// <item><description>"$.status" - filters objects by their status property</description></item>
    /// <item><description>"$.category.type" - filters by nested category type</description></item>
    /// <item><description>"$.tags[*]" - matches if any tag equals the ComparisonValue</description></item>
    /// </list>
    /// </example>
    /// <value>A JSONPath expression string relative to objects selected by Path, or null to disable filtering.</value>
    public required string? FilterPath { get; init; }

    /// <summary>
    /// Gets or sets the value used for comparison when filtering objects via <see cref="FilterPath"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This value is converted to a string and compared exactly with the string representation
    /// of values found at the <see cref="FilterPath"/>. The comparison is case-sensitive.
    /// </para>
    /// <para>
    /// Common value types include strings, numbers, and booleans. Complex objects are converted
    /// to their string representation, which may not provide meaningful comparisons.
    /// </para>
    /// <para>
    /// Set to null when <see cref="FilterPath"/> is null to disable filtering.
    /// </para>
    /// </remarks>
    /// <example>
    /// Comparison value examples:
    /// <list type="bullet">
    /// <item><description>"active" - matches string values</description></item>
    /// <item><description>42 - matches numeric values converted to "42"</description></item>
    /// <item><description>true - matches boolean values converted to "True"</description></item>
    /// </list>
    /// </example>
    /// <value>Any object that can be meaningfully converted to a string for comparison, or null when filtering is disabled.</value>
    public required object? ComparisonValue { get; init; }

    /// <summary>
    /// Gets or sets the JSONPath expression that extracts numeric values from filtered objects for aggregation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This path is evaluated relative to each object that passes the filtering criteria.
    /// All selected values are converted to double precision numbers and included in the sum calculation.
    /// Non-numeric values will cause conversion exceptions during processing.
    /// </para>
    /// <para>
    /// The path can select multiple values from each object, and all values contribute to the sum.
    /// </para>
    /// </remarks>
    /// <example>
    /// Aggregation path examples:
    /// <list type="bullet">
    /// <item><description>"$.price" - sums the price property</description></item>
    /// <item><description>"$.amounts[*]" - sums all values in the amounts array</description></item>
    /// <item><description>"$.lineItems[*].total" - sums totals from all line items</description></item>
    /// </list>
    /// </example>
    /// <value>A JSONPath expression string that selects numeric values relative to filtered objects.</value>
    public required string AggregationPath { get; init; }

    /// <summary>
    /// Gets or sets the multiplier value applied to each extracted numeric value before adding it to the sum.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This multiplier enables weighted aggregations, unit conversions, percentage calculations,
    /// and sign inversions. Each value extracted via <see cref="AggregationPath"/> is multiplied
    /// by this value before being added to the running sum.
    /// </para>
    /// <para>
    /// Common use cases include:
    /// - Applying discounts or markups (0.9 for 10% discount, 1.1 for 10% markup)
    /// - Converting units (1000 to convert from kilograms to grams)
    /// - Creating debits and credits (1.0 for credits, -1.0 for debits)
    /// - Weighting different data sources by importance
    /// </para>
    /// </remarks>
    /// <example>
    /// Multiplier value examples:
    /// <list type="bullet">
    /// <item><description>1.0 - no change (standard aggregation)</description></item>
    /// <item><description>0.85 - apply 15% discount</description></item>
    /// <item><description>-1.0 - invert sign (for subtractions)</description></item>
    /// <item><description>2.5 - apply 150% markup or weight</description></item>
    /// </list>
    /// </example>
    /// <value>A double precision number used as a multiplier for all extracted values.</value>
    public required double Value { get; init; }
}


/// <summary>
/// A transformation node that performs weighted sum aggregations across multiple filtered data sources.
/// </summary>
/// <remarks>
/// <para>
/// The SumAggregationNode provides sophisticated mathematical aggregation capabilities by processing
/// multiple data sources through a configurable pipeline of selection, filtering, and weighted summation.
/// This node is essential for complex financial calculations, inventory management, and statistical analysis.
/// </para>
/// <para>
/// Processing workflow:
/// 1. For each <see cref="SumAggregationItem"/> in the configuration
/// 2. Select objects using the item's <see cref="SumAggregationItem.Path"/>
/// 3. Apply optional filtering based on <see cref="SumAggregationItem.FilterPath"/> and <see cref="SumAggregationItem.ComparisonValue"/>
/// 4. Extract numeric values using <see cref="SumAggregationItem.AggregationPath"/>
/// 5. Multiply each value by <see cref="SumAggregationItem.Value"/>
/// 6. Add all weighted values to the running sum
/// 7. Store the final result using the inherited target path configuration
/// </para>
/// <para>
/// The node handles type conversion automatically, converting extracted values to double precision numbers.
/// Null or non-numeric values will cause processing exceptions, so ensure data integrity or implement
/// appropriate filtering to exclude invalid values.
/// </para>
/// </remarks>
/// <example>
/// Example usage for calculating net order value:
/// <code>
/// // Given data structure:
/// {
///   "orderItems": [
///     { "type": "product", "price": 100.0 },
///     { "type": "product", "price": 50.0 },
///     { "type": "service", "price": 25.0 }
///   ],
///   "adjustments": [
///     { "type": "discount", "amount": 10.0 },
///     { "type": "tax", "amount": 15.0 }
///   ]
/// }
///
/// // Configuration:
/// {
///   "TargetPath": "$.netTotal",
///   "Aggregations": [
///     {
///       "Path": "$.orderItems[*]",
///       "FilterPath": "$.type",
///       "ComparisonValue": "product",
///       "AggregationPath": "$.price",
///       "Value": 1.0
///     },
///     {
///       "Path": "$.adjustments[*]",
///       "FilterPath": "$.type",
///       "ComparisonValue": "discount",
///       "AggregationPath": "$.amount",
///       "Value": -1.0
///     }
///   ]
/// }
///
/// // Result: netTotal = (100 + 50) - 10 = 140.0
/// </code>
/// </example>
/// <param name="next">The next node in the pipeline to execute after this aggregation completes.</param>
[NodeConfiguration(typeof(SumAggregationNodeConfiguration))]
// ReSharper disable once ClassNeverInstantiated.Global
public class SumAggregationNode(NodeDelegate next) : IPipelineNode
{
    /// <summary>
    /// Processes the data context by executing weighted sum aggregations across all configured data sources.
    /// </summary>
    /// <param name="dataContext">The data context containing the JSON data to process and aggregate.</param>
    /// <param name="nodeContext">The node context containing configuration and logging capabilities.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="PipelineExecutionException">
    /// Thrown when the input data context is null.
    /// </exception>
    /// <exception cref="InvalidCastException">
    /// Thrown when values selected by <see cref="SumAggregationItem.AggregationPath"/> cannot be converted to double.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method implements the core aggregation logic by:
    /// 1. Initializing the sum accumulator to 0.0
    /// 2. Iterating through each <see cref="SumAggregationItem"/> in the configuration
    /// 3. For each item, selecting objects using the configured <see cref="SumAggregationItem.Path"/>
    /// 4. Applying optional filtering based on <see cref="SumAggregationItem.FilterPath"/> and <see cref="SumAggregationItem.ComparisonValue"/>
    /// 5. Extracting numeric values using <see cref="SumAggregationItem.AggregationPath"/>
    /// 6. Multiplying each value by <see cref="SumAggregationItem.Value"/> and adding to the sum
    /// 7. Storing the final sum at the configured target path
    /// 8. Continuing to the next node in the pipeline
    /// </para>
    /// <para>
    /// The filtering mechanism uses exact string comparison. When FilterPath is specified,
    /// all tokens selected by that path must match the ComparisonValue for the object to be included.
    /// If FilterPath is null or empty, no filtering is applied.
    /// </para>
    /// <para>
    /// All extracted values are converted to double precision numbers using JSON.NET's ToObject&lt;double&gt;() method.
    /// This supports integers, floating-point numbers, and numeric strings, but will throw exceptions
    /// for non-numeric data types.
    /// </para>
    /// </remarks>
    public async Task ProcessObjectAsync(IDataContext dataContext, INodeContext nodeContext)
    {
        var c = nodeContext.GetNodeConfiguration<SumAggregationNodeConfiguration>();
        if (dataContext.Current == null)
        {
            throw PipelineExecutionException.InputValueNull(nodeContext);
        }

        double d = 0.0;
        foreach (var sumAggregationItem in c.Aggregations)
        {
            var sourceTokens = dataContext.Current.SelectTokens(sumAggregationItem.Path).ToArray();

            foreach (var sourceToken in sourceTokens)
            {
                if (!string.IsNullOrWhiteSpace(sumAggregationItem.FilterPath) && sumAggregationItem.FilterPath != null)
                {
                    var use = sourceToken.SelectTokens(sumAggregationItem.FilterPath)
                        .All(s => s.ToString() == sumAggregationItem.ComparisonValue?.ToString());
                    if (!use)
                    {
                        continue;
                    }
                }

                sourceToken.SelectTokens(sumAggregationItem.AggregationPath).Select(s => s.ToObject<double>())
                    .ToList()
                    .ForEach(v => d += v * sumAggregationItem.Value);
            }
        }

        dataContext.SetValueByPath(c.TargetPath, c.DocumentMode, c.TargetValueKind, c.TargetValueWriteMode, d);

        await next(dataContext, nodeContext).ConfigureAwait(false);
    }
}