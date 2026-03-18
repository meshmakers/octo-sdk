using System.ComponentModel.DataAnnotations;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Data Transfer Object for updating a group.
/// </summary>
public record UpdateGroupDto
{
    /// <summary>
    ///     Name of the group.
    /// </summary>
    [Required]
    public string GroupName { get; init; } = string.Empty;

    /// <summary>
    ///     Optional description of the group.
    /// </summary>
    public string? GroupDescription { get; init; }
}
