using Meshmakers.Octo.Runtime.Contracts.Repositories.Query;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// A class representing the result of a query aggregation operation.
/// </summary>
public class QueryAggregationResult : AggregationResult
{
    /// <summary>
    /// Constructor for QueryAggregationResult.
    /// </summary>
    /// <param name="count">Count of items in the group</param>
    /// <param name="countStatistics">Count statistics for each attribute</param>
    /// <param name="minStatistics">Min statistics for each attribute</param>
    /// <param name="maxStatistics">Max statistics for each attribute</param>
    /// <param name="avgStatistics">Average value statistics for each attribute</param>
    /// <param name="sumStatistics">Sum value statistics for each attribute</param>
    /// <param name="groupBy">Grouping input for the aggregation operation</param>
    public QueryAggregationResult(long count,
        IEnumerable<StatisticsResult> countStatistics,
        IEnumerable<StatisticsResult> minStatistics,
        IEnumerable<StatisticsResult> maxStatistics,
        IEnumerable<StatisticsResult> avgStatistics,
        IEnumerable<StatisticsResult> sumStatistics,
        IEnumerable<FieldAggregationResult>? groupBy)
        : base(count, countStatistics, minStatistics, maxStatistics, avgStatistics, sumStatistics)
    {
        GroupBy = groupBy;
    }

    /// <summary>
    /// Gets the grouping input for the aggregation operation.
    /// </summary>
    public IEnumerable<FieldAggregationResult>? GroupBy { get; }
}