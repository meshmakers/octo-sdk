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
    Sum = 5,

    /// <summary>
    /// Time-weighted average (last observation carried forward) for event-based stream-data
    /// archives; yields the duty cycle on 0/100 or boolean-like signals. AB#4336.
    /// </summary>
    TimeWeightedAverage = 6,

    /// <summary>
    /// Absolute time (milliseconds) an event-based signal held the column's comparison value
    /// within the window, with LOCF semantics. AB#4336 / AB#4341.
    /// </summary>
    StateDuration = 7
}