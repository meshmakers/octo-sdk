using Newtonsoft.Json;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Represents the result of a grouping
/// </summary>
public class GroupingDto
{
    /// <summary>
    ///     Attribute names that were used to group the result
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public IEnumerable<string>? GroupByAttributeNames { get; set; }

    /// <summary>
    ///     Returns the values of the keys that were used to group the result
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public IEnumerable<object?>? Keys { get; set; }

    /// <summary>
    ///     Returns the count of items in the group
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public long? Count { get; set; }

    /// <summary>
    ///     Returns the count statistics for each attribute
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public IEnumerable<StatisticsDto>? CountStatistics { get; set; }

    /// <summary>
    ///     Returns the min statistics for each attribute
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public IEnumerable<StatisticsDto>? MinStatistics { get; set; }

    /// <summary>
    ///     Returns the max statistics for each attribute
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public IEnumerable<StatisticsDto>? MaxStatistics { get; set; }

    /// <summary>
    ///     Returns the average value statistics for each attribute
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public IEnumerable<StatisticsDto>? AvgStatistics { get; set; }
}