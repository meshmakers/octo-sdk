namespace Meshmakers.Octo.Sdk.ServiceClient.Authentication;

/// <summary>
/// Represents the authentication data.
/// </summary>
public class AuthenticationData
{
    /// <summary>
    /// Returns the access token.
    /// </summary>
    public string? AccessToken { get; set; }
    
    /// <summary>
    /// Returns the refresh token.
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// The time when the access token expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}
