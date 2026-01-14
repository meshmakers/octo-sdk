namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Defines the aggregation types for runtime queries.
/// </summary>
public enum AggregationTypesDto
{
    /// <summary>
    /// No aggregation.
    /// </summary>
    None = 0,

    /// <summary>
    /// Counts the number of items.
    /// </summary>
    Count = 1,

    /// <summary>
    /// Minimum value.
    /// </summary>
    Minimum = 2,

    /// <summary>
    /// Maximum value.
    /// </summary>
    Maximum = 3,

    /// <summary>
    /// Average value.
    /// </summary>
    Average = 4,

    /// <summary>
    /// Sum of values.
    /// </summary>
    Sum = 5
}