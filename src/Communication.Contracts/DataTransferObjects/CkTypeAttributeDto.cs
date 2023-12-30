using System.Text.Json.Serialization;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Represents an attribute that corresponds to a type, association or record.
/// </summary>
public class CkTypeAttributeDto
{
    /// <summary>
    /// Gets or sets the CK attribute id.
    /// </summary>
    public CkId<CkAttributeId> CkAttributeId { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the attribute.
    /// </summary>
    public string AttributeName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the attribute type
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AttributeValueTypesDto AttributeValueType { get; set; }

    /// <summary>
    /// If auto completion is enabled, this property defines the attribute that is used as a reference for the auto completion values.
    /// </summary>
    public string? AutoIncrementReference { get; set; }

    /// <summary>
    /// Gets or sets a list of values that are used for auto completion.
    /// </summary>
    public IReadOnlyCollection<object>? AutoCompleteValues { get; set; }

    /// <summary>
    /// If true, the attribute is optional, that means it can be null
    /// </summary>
    public bool IsOptional { get; set; }
    
    /// <summary>
    /// Reference to construction kit attribute definitino
    /// </summary>
    public CkAttributeDto? Attribute { get; set; }
}