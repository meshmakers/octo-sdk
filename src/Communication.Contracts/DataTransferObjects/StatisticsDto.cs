using Newtonsoft.Json;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Represents a statistics value for a grouping.
/// </summary>
public class StatisticsDto
{
    /// <summary>
    ///     Attribute name of the calculated statistics.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [JsonRequired]
    public string AttributeName { get; set; } = null!;

    /// <summary>
    ///     Value of the calculated statistics.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public object? Value { get; set; }
}