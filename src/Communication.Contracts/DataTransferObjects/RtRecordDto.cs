using System.Text.Json.Serialization;
using Meshmakers.Octo.ConstructionKit.Contracts;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Base class for all runtime records DTOs
/// </summary>
public class RtRecordDto : GraphQlDto
{
    /// <summary>
    ///     Gets or sets the type id of the entity
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public CkId<CkRecordId> CkRecordId { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the properties of the entity
    /// </summary>
    [JsonExtensionData]
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public IDictionary<string, object>? Properties { get; set; }
}