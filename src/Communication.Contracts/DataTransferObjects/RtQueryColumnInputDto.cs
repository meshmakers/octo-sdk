using System.Text.Json.Serialization;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Defines a runtime query column with aggregation type and attribute path.
/// </summary>
public class RtQueryColumnInputDto
{
    /// <summary>
    /// Gets or sets the aggregation type for the column.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AggregationInputTypesDto AggregationType { get; set; }

    /// <summary>
    /// Gets or sets the attribute path for the column.
    /// </summary>
    public string AttributePath { get; set; } = null!;
}