using System.Text.Json.Serialization;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.ConstructionKit.Contracts.Serialization;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Base class for all runtime records DTOs
/// </summary>
public class RtRecordDto : GraphQlDto
{
    /// <summary>
    ///     Gets or sets the record id
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonConverter(typeof(CkIdRecordIdConverter))]
    public CkId<CkRecordId> CkRecordId { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the attributes of the record
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public IList<RtEntityAttributeDto>? Attributes { get; set; }
}