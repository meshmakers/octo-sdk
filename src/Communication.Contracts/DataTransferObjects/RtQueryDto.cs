using Meshmakers.Octo.ConstructionKit.Contracts;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Represents a runtime query
/// </summary>
public class RtQueryDto : GraphQlDto
{
    /// <summary>
    /// Gets or sets the query runtime identifier
    /// </summary>
    public OctoObjectId QueryRtId { get; set; }
    
    /// <summary>
    ///     Gets or sets the attributes of the entity
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public required IList<RtQueryColumnDto> Columns { get; set; }
}