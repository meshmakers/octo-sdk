using System.ComponentModel.DataAnnotations;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Identity provider configuration specifically for Azure Entry ID
/// </summary>
public class AzureEntraIdProviderDto : IdentityProviderDto
{
    /// <summary>
    ///     The Tenant ID for the Azure Entra ID.
    /// </summary>
    [Required]
    public string TenantId { get; set; } = null!;

    /// <summary>
    ///     Authority (default value: https://login.microsoftonline.com).
    /// </summary>
    public string? Authority { get; set; }

    /// <summary>
    ///     Client ID (group Azure Entra ID).
    /// </summary>
    [Required]
    public string ClientId { get; set; } = null!;

    /// <summary>
    ///     Client Secret (group Entra ID). Required on create; on update, omit (null or empty) to
    ///     preserve the existing secret unchanged.
    /// </summary>
    public string? ClientSecret { get; set; }
}