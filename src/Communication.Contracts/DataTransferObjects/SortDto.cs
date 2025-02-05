using System.Text.Json.Serialization;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Data transfer object for sorting.
/// </summary>
public class SortDto
{
    /// <summary>
    ///     Attribute path to sort by.
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public string AttributePath { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the sort order.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public SortOrdersDto SortOrder { get; set; }
}