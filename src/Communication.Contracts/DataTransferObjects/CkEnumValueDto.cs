using System.Text.Json.Serialization;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Defines enum values for an enum type.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class CkEnumValueDto
{
    /// <summary>
    ///     Key of the enum value.
    /// </summary>
    [JsonRequired]
    public int Key { get; set; }

    /// <summary>
    ///     Name of the enum value.
    /// </summary>
    [JsonRequired]
    public string Name { get; set; } = null!;

    /// <summary>
    ///     An optional description of the enum value.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    ///     Indicates that the current enum value is an extension to the original enum.
    /// </summary>
    public bool IsExtension { get; set; } = false;

}