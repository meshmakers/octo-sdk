namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Defines the aggregation types for runtime queries as input type
/// </summary>
public enum AggregationInputTypesDto
{
    /// <summary>
    /// Counts the number of items.
    /// </summary>
    Count = 0,

    /// <summary>
    /// Minimum value.
    /// </summary>
    Minimum = 1,

    /// <summary>
    /// Maximum value.
    /// </summary>
    Maximum = 2,

    /// <summary>
    /// Average value.
    /// </summary>
    Average = 3,

    /// <summary>
    /// Sum of values.
    /// </summary>
    Sum = 4
}