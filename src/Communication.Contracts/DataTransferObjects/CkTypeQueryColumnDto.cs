using System.Diagnostics;
using System.Text.Json.Serialization;
using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Represents a possible column in a query result.
/// </summary>
[DebuggerDisplay("{" + nameof(AttributePath) + "}")]
public class CkTypeQueryColumnDto
{
    /// <summary>
    ///     Gets or sets the path of the attribute.
    /// </summary>
    public string AttributePath { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the attribute type
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AttributeValueTypesDto AttributeValueType { get; set; }

    /// <summary>
    ///     Gets or sets the description of the attribute.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     Gets or sets whether this column is available in stream data queries.
    /// </summary>
    public bool IsDataStream { get; set; }
}