namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Represents the input for aggregation operations on attributes.
/// </summary>
public class AggregationInputDto
{
    /// <summary>
    ///     Attribute paths whose existence to count, NULL values are not counted.
    /// </summary>
    public IEnumerable<string>? CountAttributePaths { get; set; }

    /// <summary>
    ///     Attributes paths whose maximum value is to be determined.
    /// </summary>
    public IEnumerable<string>? MaxValueAttributePaths { get; set; }

    /// <summary>
    ///     Attributes paths whose minimum value is to be determined.
    /// </summary>
    public IEnumerable<string>? MinValueAttributePaths { get; set; }

    /// <summary>
    ///     Attributes paths whose average value is to be determined.
    /// </summary>
    public IEnumerable<string>? AvgAttributePaths { get; set; }

    /// <summary>
    ///     Attributes paths whose sum value is to be determined.
    /// </summary>
    public IEnumerable<string>? SumAttributePaths { get; set; }
}