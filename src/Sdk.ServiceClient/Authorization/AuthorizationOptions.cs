namespace Meshmakers.Octo.Common.Shared.Authorization;

/// <summary>
/// Represents the options for the authorization client.
/// </summary>
public class AuthorizationOptions
{
    /// <summary>
    /// Issuer URI of the authorization server.
    /// </summary>
    public string IssuerUri { get; set; } = null!;
    
    /// <summary>
    /// Client ID of the authorization client.
    /// </summary>
    public string ClientId { get; set; } = null!;
    
    /// <summary>
    /// Client secret of the authorization client.
    /// </summary>
    public string? ClientSecret { get; set; }
}