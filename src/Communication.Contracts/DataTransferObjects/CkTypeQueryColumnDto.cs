using System.Text.Json.Serialization;
using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Represents a possible column in a query result.
/// </summary>
public class CkTypeQueryColumnDto
{
    /// <summary>
    ///     Gets or sets the name of the attribute.
    /// </summary>
    public string AttributeName { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the attribute type
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AttributeValueTypesDto AttributeValueType { get; set; }
}