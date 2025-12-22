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

    /// <summary>
    ///     If true, resolve enum integer values to their label names in groupBy keys.
    ///     Defaults to true.
    /// </summary>
    public bool ResolveEnumValuesToNames { get; set; } = true;
}