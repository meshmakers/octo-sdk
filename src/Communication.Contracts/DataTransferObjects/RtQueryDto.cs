using System.Text.Json.Serialization;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.ConstructionKit.Contracts.Serialization;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Represents a runtime query
/// </summary>
public class RtQueryDto : GraphQlDto
{
    /// <summary>
    ///     Gets or sets the query runtime identifier
    /// </summary>
    public OctoObjectId QueryRtId { get; set; }

    /// <summary>
    ///     Gets or sets the ck type id of the associated type of the query
    /// </summary>
    [JsonConverter(typeof(CkIdTypeIdConverter))]
    public CkId<CkTypeId> AssociatedCkTypeId { get; set; } = null!;
    
    /// <summary>
    ///     Gets or sets the attributes of the entity
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public required IList<RtQueryColumnDto> Columns { get; set; }
}