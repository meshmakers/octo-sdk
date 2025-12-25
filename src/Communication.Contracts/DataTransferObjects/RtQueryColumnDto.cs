using System.Text.Json.Serialization;
using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Represents a query column of a runtime query
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class RtQueryColumnDto : GraphQlDto
{
    /// <summary>
    ///     Gets or sets the attribute path.
    /// </summary>
    public required string AttributePath { get; set; }
    
    /// <summary>
    ///     Gets or sets the attribute data type.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required AttributeValueTypesDto AttributeValueType { get; set; }

    /// <summary>
    /// Gets or sets the aggregation type.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required AggregationTypesDto AggregationType { get; set; }
}