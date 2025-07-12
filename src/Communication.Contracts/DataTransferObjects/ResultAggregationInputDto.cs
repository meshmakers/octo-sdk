namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Represents the input for result aggregation operations.
/// </summary>
public class ResultAggregationInputDto : AggregationInputDto
{
    /// <summary>
    /// Grouping input for the aggregation operation.
    /// </summary>
    public FieldGroupByAggregationInputDto? GroupBy { get; set; } = null;
}