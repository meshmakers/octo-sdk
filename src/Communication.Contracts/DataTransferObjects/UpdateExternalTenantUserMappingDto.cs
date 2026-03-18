namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Data Transfer Object for updating an external tenant user mapping.
/// </summary>
public record UpdateExternalTenantUserMappingDto
{
    /// <summary>
    ///     Optional list of role IDs to assign.
    /// </summary>
    public List<string>? RoleIds { get; init; }
}
