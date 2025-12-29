using System.Text.Json.Serialization;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.ConstructionKit.Contracts.Serialization;
using Meshmakers.Octo.Runtime.Contracts.Serialization;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Represents an entity row in a query result
/// </summary>
public interface IRtQueryRowDto
{
    /// <summary>
    ///     Gets or sets the type id of the entity
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonConverter(typeof(RtCkIdTypeIdConverter))]
    public RtCkId<CkTypeId> CkTypeId { get; set; }

    /// <summary>
    ///     Gets or sets the cells of the entity row
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public IList<RtQueryCellDto>? Cells { get; set; }
}

/// <summary>
///     Represents an entity row in a query result
/// </summary>
public class RtSimpleQueryRowDto : GraphQlDto, IRtQueryRowDto
{
    /// <summary>
    ///     Gets or sets the type id of the entity
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonConverter(typeof(RtCkIdTypeIdConverter))]
    public RtCkId<CkTypeId> CkTypeId { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the cells of the entity row
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public IList<RtQueryCellDto>? Cells { get; set; }

    /// <summary>
    ///     Gets or sets the id of the entity
    /// </summary>
    [JsonConverter(typeof(OctoObjectIdConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [Newtonsoft.Json.JsonConverter(typeof(NewtonOctoObjectIdConverter))]
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
    ///     Gets or sets the well known name of the entity
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? RtWellKnownName { get; set; }

    /// <summary>
    ///     Gets or sets the version of the entity
    /// </summary>
    public ulong RtVersion { get; set; }
}


/// <summary>
///     Represents an entity row in a query result
/// </summary>
public class RtAggregationQueryRowDto : GraphQlDto, IRtQueryRowDto
{
    /// <summary>
    ///     Gets or sets the type id of the entity
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonConverter(typeof(RtCkIdTypeIdConverter))]
    public RtCkId<CkTypeId> CkTypeId { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the cells of the entity row
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public IList<RtQueryCellDto>? Cells { get; set; }
}

/// <summary>
///     DTO for a grouping aggregation query row result
/// </summary>
public class RtGroupingAggregationQueryRowDto : GraphQlDto, IRtQueryRowDto
{
    /// <summary>
    ///     Gets or sets the type id of the entity
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonConverter(typeof(RtCkIdTypeIdConverter))]
    public RtCkId<CkTypeId> CkTypeId { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the cells of the entity row
    /// </summary>
    public IList<RtQueryCellDto>? Cells { get; set; }
}