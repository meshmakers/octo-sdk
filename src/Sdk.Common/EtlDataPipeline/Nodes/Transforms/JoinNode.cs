using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.Services;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;

/// <summary>
/// Configuration for a join node that performs inner joins between source data and lookup arrays.
/// </summary>
/// <remarks>
/// The join node performs an inner join operation similar to SQL joins, matching records from the source data
/// with records from a lookup array based on key values. All matching records from the lookup array are
/// added to the source records at the specified target path.
/// </remarks>
/// <example>
/// Example configuration to join orders with their order items:
/// <code>
/// {
///   "Path": "$.orders[*]",
///   "KeyPath": "$.orderId",
///   "JoinPath": "$.orderItems[*]",
///   "JoinKeyPath": "$.orderId",
///   "ItemPath": "$.items"
/// }
/// </code>
/// This will add all matching order items to each order under the "items" property.
/// </example>
[NodeName("Join", 1)]
public record JoinNodeConfiguration : PathNodeConfiguration
{
    /// <summary>
    /// Gets or sets the JSONPath to the key field in the source data that will be used for matching.
    /// </summary>
    /// <remarks>
    /// This path is relative to each item selected by the <see cref="PathNodeConfiguration.Path"/> property.
    /// The value at this path will be compared with values from the <see cref="JoinKeyPath"/> to find matches.
    /// </remarks>
    /// <example>
    /// For source data like { "orderId": "123", "customerName": "John" },
    /// use "$.orderId" to match on the order ID.
    /// </example>
    [PropertyGroup("Paths", 2, "jsonpath")]
    public required string KeyPath { get; set; }

    /// <summary>
    /// Gets or sets the JSONPath to the array of items that will be used as the lookup/join source.
    /// </summary>
    /// <remarks>
    /// This path selects the array of records that will be searched for matching keys.
    /// Each item in this array will be tested against the source records for potential joins.
    /// The path is evaluated against the root data context.
    /// </remarks>
    /// <example>
    /// Use "$.orderItems[*]" to select all items from an orderItems array for joining.
    /// </example>
    [PropertyGroup("Paths", 3, "jsonpath")]
    public required string JoinPath { get; set; }

    /// <summary>
    /// Gets or sets the JSONPath to the key field in the join array items that will be matched against the source key.
    /// </summary>
    /// <remarks>
    /// This path is relative to each item selected by the <see cref="JoinPath"/> property.
    /// The value at this path will be compared with values from the <see cref="KeyPath"/> to find matches.
    /// When values match, the entire join record will be included in the result.
    /// </remarks>
    /// <example>
    /// For join data like { "orderId": "123", "productName": "Widget", "quantity": 2 },
    /// use "$.orderId" to match on the order ID.
    /// </example>
    [PropertyGroup("Paths", 4, "jsonpath")]
    public required string JoinKeyPath { get; set; }

    /// <summary>
    /// Gets or sets the JSONPath where the array of matched join records will be stored in the source data.
    /// </summary>
    /// <remarks>
    /// This path is relative to each source item selected by the <see cref="PathNodeConfiguration.Path"/> property.
    /// All records from the join array that have matching key values will be stored as a JSON array at this location.
    /// If no matches are found, an empty array will be created.
    /// </remarks>
    /// <example>
    /// Use "$.items" to store the joined records in an "items" property of each source record.
    /// </example>
    [PropertyGroup("Paths", 5, "jsonpath")]
    public required string ItemPath { get; set; }
}

/// <summary>
/// A transformation node that performs inner join operations between source data and lookup arrays based on matching key values.
/// </summary>
/// <remarks>
/// <para>
/// The JoinNode implements a many-to-many inner join operation, similar to SQL joins. For each item in the source data,
/// it finds all matching records in the join array where the key values are equal (string comparison).
/// All matching records are collected into a new array and stored at the specified target path in the source record.
/// </para>
/// <para>
/// The join operation follows these steps:
/// 1. Select source records using the <see cref="PathNodeConfiguration.Path"/>
/// 2. Select join records using the <see cref="JoinNodeConfiguration.JoinPath"/>
/// 3. For each source record, extract the key value using <see cref="JoinNodeConfiguration.KeyPath"/>
/// 4. Find all join records where <see cref="JoinNodeConfiguration.JoinKeyPath"/> matches the source key
/// 5. Store all matching join records as an array at <see cref="JoinNodeConfiguration.ItemPath"/>
/// </para>
/// <para>
/// If no matching records are found for a source item, an empty array is stored at the target path.
/// String comparison is case-sensitive and uses exact matching.
/// </para>
/// </remarks>
/// <example>
/// Given source data:
/// <code>
/// {
///   "orders": [
///     { "orderId": "123", "customerName": "John" },
///     { "orderId": "456", "customerName": "Jane" }
///   ],
///   "orderItems": [
///     { "orderId": "123", "productName": "Widget", "quantity": 2 },
///     { "orderId": "123", "productName": "Gadget", "quantity": 1 },
///     { "orderId": "456", "productName": "Tool", "quantity": 3 }
///   ]
/// }
/// </code>
/// With configuration Path="$.orders[*]", KeyPath="$.orderId", JoinPath="$.orderItems[*]",
/// JoinKeyPath="$.orderId", ItemPath="$.items", the result will be:
/// <code>
/// {
///   "orders": [
///     {
///       "orderId": "123",
///       "customerName": "John",
///       "items": [
///         { "orderId": "123", "productName": "Widget", "quantity": 2 },
///         { "orderId": "123", "productName": "Gadget", "quantity": 1 }
///       ]
///     },
///     {
///       "orderId": "456",
///       "customerName": "Jane",
///       "items": [
///         { "orderId": "456", "productName": "Tool", "quantity": 3 }
///       ]
///     }
///   ]
/// }
/// </code>
/// </example>
/// <param name="next">The next node in the pipeline to execute after this transformation.</param>
[NodeConfiguration(typeof(JoinNodeConfiguration))]
public class JoinNode(NodeDelegate next) : IPipelineNode
{
    /// <summary>
    /// Processes the data context by performing join operations between source data and lookup arrays.
    /// </summary>
    /// <param name="dataContext">The data context containing the JSON data to process.</param>
    /// <param name="nodeContext">The node context containing configuration and logging capabilities.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="PipelineExecutionException">
    /// Thrown when:
    /// - The input data context is null
    /// - No source data is found at the specified <see cref="PathNodeConfiguration.Path"/>
    /// - No join data is found at the specified <see cref="JoinNodeConfiguration.JoinPath"/>
    /// - A source record has no key value at the specified <see cref="JoinNodeConfiguration.KeyPath"/>
    /// </exception>
    /// <remarks>
    /// This method performs the following operations:
    /// 1. Validates that the data context contains data
    /// 2. Selects source and join arrays using their respective JSONPath expressions
    /// 3. For each source record, extracts the key value and finds matching join records
    /// 4. Creates a new array containing all matching join records
    /// 5. Stores the joined array at the specified target path in the source record
    /// 6. Continues to the next node in the pipeline
    /// </remarks>
    public async Task ProcessObjectAsync(IDataContext dataContext, INodeContext nodeContext)
    {
        var c = nodeContext.GetNodeConfiguration<JoinNodeConfiguration>();
        if (dataContext.Current == null)
        {
            throw PipelineExecutionException.InputValueNull(nodeContext);
        }

        var sourceTokens = dataContext.Current.SelectTokens(c.Path).ToArray();
        var joinTokens = dataContext.Current.SelectTokens(c.JoinPath).ToArray();

        if (!sourceTokens.Any())
        {
            throw PipelineExecutionException.ValueNotSet(nodeContext, c.Path);
        }
        if (!joinTokens.Any())
        {
            foreach (var sourceToken in sourceTokens)
            {
                sourceToken.ReplaceNested(c.ItemPath, new JArray());
            }

            await next(dataContext, nodeContext).ConfigureAwait(false);
            return;
        }

        foreach (var sourceToken in sourceTokens)
        {
            var sourceValue = sourceToken.SelectToken(c.KeyPath)?.ToString();
            if (string.IsNullOrEmpty(sourceValue))
            {
                throw PipelineExecutionException.ValueNotSet(nodeContext, c.KeyPath);
            }

            var joinedItems = joinTokens
                .Where(j => j.SelectToken(c.JoinKeyPath)?.ToString() == sourceValue)
                .ToList();
            var newArray = new JArray(joinedItems);
            sourceToken.ReplaceNested(c.ItemPath, newArray);
        }

        await next(dataContext, nodeContext).ConfigureAwait(false);
    }
}