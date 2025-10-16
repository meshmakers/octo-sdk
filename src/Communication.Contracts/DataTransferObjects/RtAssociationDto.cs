using System.Text.Json.Serialization;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.ConstructionKit.Contracts.Serialization;
using Meshmakers.Octo.Runtime.Contracts.Serialization;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Base class for all runtime entity DTOs
/// </summary>
public class RtAssociationDto : GraphQlDto
{
    /// <summary>
    ///     Gets or sets the origin runtime entity id
    /// </summary>
    [JsonConverter(typeof(OctoObjectIdConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [Newtonsoft.Json.JsonConverter(typeof(NewtonOctoObjectIdConverter))]
    public OctoObjectId OriginRtId { get; set; }

    /// <summary>
    ///     Gets or sets the type id of the origin runtime entity
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonConverter(typeof(RtCkIdTypeIdConverter))]
    public required RtCkId<CkTypeId> OriginCkTypeId { get; set; }

    /// <summary>
    ///     Gets or sets the target runtime entity id
    /// </summary>
    [JsonConverter(typeof(OctoObjectIdConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [Newtonsoft.Json.JsonConverter(typeof(NewtonOctoObjectIdConverter))]
    public OctoObjectId TargetRtId { get; set; }

    /// <summary>
    ///     Gets or sets the type id of the target runtime entity
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonConverter(typeof(RtCkIdTypeIdConverter))]
    public required RtCkId<CkTypeId> TargetCkTypeId { get; set; }

    /// <summary>
    ///     Gets or sets the type id of the association
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonConverter(typeof(RtCkIdTypeIdConverter))]
    public required RtCkId<CkAssociationRoleId> CkAssociationRoleId { get; set; }

    /// <summary>
    ///     Gets or sets the attributes of the entity
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public IList<RtEntityAttributeDto>? Attributes { get; set; }
}