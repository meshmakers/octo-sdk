using System.Text.Json.Serialization;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.ConstructionKit.Contracts.Serialization;

namespace Meshmakers.Octo.Common.Shared.DataTransferObjects;

/// <summary>
/// Base class for all runtime entity DTOs
/// </summary>
public class RtEntityDto : GraphQlDto
{
    /// <summary>
    /// Gets or sets the id of the entity
    /// </summary>
    [JsonConverter(typeof(OctoObjectIdConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public OctoObjectId RtId { get; set; }

    /// <summary>
    ///     Returns the creation date time
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public DateTime? RtCreationDateTime { get; set; }

    /// <summary>
    ///     Returns the last change date time
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public DateTime? RtChangedDateTime { get; set; }

    /// <summary>
    /// Gets or sets the type id of the entity
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public CkId<CkTypeId> CkTypeId { get; set; }

    /// <summary>
    /// Gets or sets the well known name of the entity
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? RtWellKnownName { get; set; }

    /// <summary>
    /// Gets or sets the properties of the entity
    /// </summary>
    [JsonExtensionData]
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public IDictionary<string, object>? Properties { get; set; }
}