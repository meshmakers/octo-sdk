using System.Text.Json.Serialization;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts.Serialization;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Represents a construction kit attribute
/// </summary>
public class CkAttributeDto
{
    /// <summary>
    ///     Construction kit attribute id
    /// </summary>
    public CkId<CkAttributeId> CkAttributeId { get; set; } = null!;

    /// <summary>
    ///     Value type of the attribute
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AttributeValueTypesDto AttributeValueType { get; set; }

    /// <summary>
    ///     Defines the record of the attribute if the value type is a record.
    /// </summary>
    [JsonConverter(typeof(CkIdRecordIdConverter))]
    public CkId<CkRecordId>? ValueCkRecordId { get; set; }

    /// <summary>
    ///     Defines the enum of the attribute if the value type is a enum.
    /// </summary>
    [JsonConverter(typeof(CkIdEnumIdConverter))]
    public CkId<CkEnumId>? ValueCkEnumId { get; set; }

    /// <summary>
    ///     Gets or sets default values
    /// </summary>
    public ICollection<object>? DefaultValues { get; set; }

    /// <summary>
    ///     An optional description of the attribute
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     Optional meta data of the attribute
    /// </summary>
    public ICollection<CkAttributeMetaDataDto>? MetaData { get; set; }
}