using System.ComponentModel.DataAnnotations;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Data Transfer Object for creating a group.
/// </summary>
public record CreateGroupDto
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

    /// <summary>
    ///     Optional list of role IDs to assign to the group.
    /// </summary>
    public List<string>? RoleIds { get; init; }
}
