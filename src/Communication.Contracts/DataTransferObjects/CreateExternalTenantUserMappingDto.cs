using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

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

    /// <summary>
    ///     Optional ids of target-tenant groups the mapping becomes a member of (AB#8340). The mapped user gets exactly
    ///     the roles of these groups plus <see cref="RoleIds" />. <c>null</c> or empty keeps the behaviour without groups.
    ///     An id that is no group of the target tenant is refused with <c>400</c>, code
    ///     <see cref="ApiErrors.CrossTenantGrantProblemCodes.UnknownGroup" />. Not sent when <c>null</c>, so requests of
    ///     existing callers keep their wire shape.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? GroupIds { get; init; }
}
