namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     The one user a source user lookup found (AB#8340). Carries what a mapping needs
///     (<see cref="CreateExternalTenantUserMappingDto" />) and what the dialog shows; no e-mail address, no phone number.
/// </summary>
public record SourceUserLookupResultDto
{
    /// <summary>
    ///     The source tenant the user belongs to.
    /// </summary>
    public string SourceTenantId { get; init; } = string.Empty;

    /// <summary>
    ///     The user's id in the source tenant (<see cref="CreateExternalTenantUserMappingDto.SourceUserId" />).
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    ///     The user's name in the source tenant (<see cref="CreateExternalTenantUserMappingDto.SourceUserName" />).
    /// </summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>
    ///     The user's display name, if the source tenant has one for the user.
    /// </summary>
    public string? DisplayName { get; init; }
}
