using System.Text.Json.Serialization;

// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace Meshmakers.Octo.Common.Shared.DataTransferObjects;

/// <summary>
/// Represents a field filter for input
/// </summary>
public class FieldFilterDto
{
    /// <summary>
    /// Attribute name
    /// </summary>
    public string AttributeName { get; set; } = null!;

    /// <summary>
    /// Comparison operator
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public FieldFilterOperatorDto Operator { get; set; }

    /// <summary>
    /// Comparison value
    /// </summary>
    public object? ComparisonValue { get; set; }
}