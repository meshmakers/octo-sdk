using System.Diagnostics;
using System.Text.Json.Serialization;
using Meshmakers.Octo.ConstructionKit.Contracts;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Describes a construction kit enum that is used as enum type of attribute
/// </summary>
[DebuggerDisplay("{" + nameof(CkEnumId) + "}")]
public class CkEnumDto
{
    /// <summary>
    ///     Creates a new instance of <see cref="CkEnumDto" />.
    /// </summary>
    public CkEnumDto()
    {
        Values = new List<CkEnumValueDto>();
    }

    /// <summary>
    ///     Gets or sets the construction kit id
    /// </summary>
    [JsonRequired]
    public CkId<CkEnumId> CkEnumId { get; set; } = null!;
    
    /// <summary>
    ///     An optional description of the enum
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     When true the enum is handles as flags enum
    /// </summary>
    public bool UseFlags { get; set; }
    
    /// <summary>
    ///     When true the enum is extensible using the API    
    /// </summary>
    public bool IsExtensible { get; set; } = false;

    /// <summary>
    ///     Values of the enum
    /// </summary>
    public ICollection<CkEnumValueDto> Values { get; set; }
}