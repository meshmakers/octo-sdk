namespace Meshmakers.Octo.Sdk.ServiceClient.Authorization;

/// <summary>
///     Represents the options for the authorization client.
/// </summary>
public class AuthorizationOptions
{
    /// <summary>
    ///     Issuer URI of the authorization server.
    /// </summary>
    public string IssuerUri { get; set; } = null!;

    /// <summary>
    ///     Client ID of the authorization client.
    /// </summary>
    public string ClientId { get; set; } = null!;

    /// <summary>
    ///     Client secret of the authorization client.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    ///     Tenant ID to include as acr_values in authorization requests.
    /// </summary>
    public string? TenantId { get; set; }
}