using System.ComponentModel.DataAnnotations;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Data Transfer Object for creating an external tenant user mapping.
/// </summary>
public record CreateExternalTenantUserMappingDto
{
    /// <summary>
    ///     The source tenant ID.
    /// </summary>
    [Required]
    public string SourceTenantId { get; init; } = string.Empty;

    /// <summary>
    ///     The source user ID.
    /// </summary>
    [Required]
    public string SourceUserId { get; init; } = string.Empty;

    /// <summary>
    ///     The source user name.
    /// </summary>
    [Required]
    public string SourceUserName { get; init; } = string.Empty;

    /// <summary>
    ///     Optional list of role IDs to assign.
    /// </summary>
    public List<string>? RoleIds { get; init; }
}
