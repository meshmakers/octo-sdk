using System.Text.Json.Serialization;

// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Represents a field filter for input
/// </summary>
public class FieldFilterDto
{
    /// <summary>
    ///     Path of the attribute to filter
    /// </summary>
    public string AttributePath { get; set; } = null!;

    /// <summary>
    ///     Comparison operator
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public FieldFilterOperatorDto Operator { get; set; }

    /// <summary>
    ///     Comparison value
    /// </summary>
    public object? ComparisonValue { get; set; }

    /// <summary>
    ///     Secondary comparison value for operators that take two inputs, e.g. <c>Between</c>.
    /// </summary>
    public object? SecondaryValue { get; set; }
}