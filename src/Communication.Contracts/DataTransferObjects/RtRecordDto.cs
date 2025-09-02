using System.Text.Json.Serialization;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.ConstructionKit.Contracts.Serialization;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Base class for all runtime records DTOs
/// </summary>
public class RtRecordDto : RtTypeWithAttributesDto
{
    /// <summary>
    ///     Gets or sets the record id
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonConverter(typeof(CkIdRecordIdConverter))]
    public CkId<CkRecordId> CkRecordId { get; set; } = null!;


}