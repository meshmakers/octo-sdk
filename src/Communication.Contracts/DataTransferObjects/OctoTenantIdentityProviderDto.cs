using System.ComponentModel.DataAnnotations;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Identity provider configuration for cross-tenant authentication via a parent tenant.
/// </summary>
public class OctoTenantIdentityProviderDto : IdentityProviderDto
{
    /// <summary>
    ///     The ID of the parent tenant to authenticate against.
    /// </summary>
    [Required]
    [MinLength(1)]
    public string? ParentTenantId { get; set; }
}
