using System.Text.Json.Serialization;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.ConstructionKit.Contracts.Serialization;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Represents a transient runtime query
/// </summary>
public class RtTransientQueryDto : GraphQlDto
{
    /// <summary>
    ///     Gets or sets the ck type id of the associated type for the query
    /// </summary>
    [JsonConverter(typeof(CkIdTypeIdConverter))]
    public CkId<CkTypeId> AssociatedCkTypeId { get; set; } = null!;
    
    /// <summary>
    ///     Gets or sets the attributes of the entity
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public required IList<RtQueryColumnDto> Columns { get; set; }
}