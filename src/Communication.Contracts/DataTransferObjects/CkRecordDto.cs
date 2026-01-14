using System.Diagnostics;
using System.Text.Json.Serialization;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.ConstructionKit.Contracts.Serialization;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Describes a construction kit record that is used as structured type of an attribute
/// </summary>
[DebuggerDisplay("{" + nameof(CkRecordId) + "}")]
public class CkRecordDto
{
    /// <summary>
    /// Gets or sets the construction kit id, e.g. System-1.0.0/Record-2
    /// </summary>
    [JsonRequired]
    public CkId<CkRecordId> CkRecordId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the runtime construction kit id, that has no versioning information for model id,
    /// but for the element id - e.g. System/Record-2
    /// </summary>
    public RtCkId<CkRecordId> RtCkRecordId { get; set; } = null!;
    
    /// <summary>
    ///     An optional description of the record
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     Defines the base record of this record.
    /// </summary>
    [JsonConverter(typeof(CkIdRecordIdConverter))]
    public CkId<CkRecordId>? DerivedFromCkRecordId { get; set; }

    /// <summary>
    ///     If true, the type cannot be inherited again
    /// </summary>
    public bool IsFinal { get; set; }

    /// <summary>
    ///     If true, the type cannot be instantiated by a runtime entity
    /// </summary>
    public bool IsAbstract { get; set; }

    /// <summary>
    ///     Gets or sets a list of attributes
    /// </summary>
    public ICollection<CkTypeAttributeDto>? Attributes { get; set; }
}