namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Represents for aggregations a field group by clause
/// </summary>
public class FieldGroupByAggregationInputDto : AggregationInputDto
{
    /// <summary>
    ///     Attribute paths to group by
    /// </summary>
    public IEnumerable<string> GroupByAttributePaths { get; set; } = null!;
}