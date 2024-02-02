using Meshmakers.Octo.Communication.Contracts;
using Meshmakers.Octo.Sdk.ServiceClient.Authorization;

namespace Meshmakers.Octo.Sdk.ServiceClient.Authentication;

/// <summary>
///     Interface for the authenticator client.
/// </summary>
public interface IAuthenticatorClient : IAuthorizationClient
{
    /// <summary>
    ///     Ensures that the user is authenticated, if not it will try to refresh the token.
    /// </summary>
    /// <param name="refreshToken">The refresh token.</param>
    /// <param name="accessToken">The current access token</param>
    /// <returns>Authentication data received by the identity provider</returns>
    Task<EnsureAuthenticatedData> EnsureAuthenticatedAsync(string refreshToken, string accessToken);

    /// <summary>
    ///     Requests authentication using device authorization.
    /// </summary>
    /// <param name="apiScopes">The requested api scopes</param>
    /// <param name="customScopes">Optional additional custom scopes to be added to auth token</param>
    /// <returns></returns>
    Task<DeviceAuthenticationRequestData> RequestDeviceAuthorizationAsync(ApiScopes apiScopes,
        IEnumerable<string>? customScopes = null);

    /// <summary>
    ///     Requests authentication using a device code received by <seealso cref="RequestDeviceAuthorizationAsync" />
    /// </summary>
    /// <param name="deviceCode">The device code</param>
    /// <returns>Authentication data received by the identity provider</returns>
    Task<DeviceAuthenticationData> RequestDeviceTokenAsync(string deviceCode);

    /// <summary>
    ///     Requests authentication using client credentials.
    /// </summary>
    /// <param name="apiScopes">The requested api scopes</param>
    /// <param name="defaultScopes">The requested default scopes</param>
    /// <param name="customScopes">Optional additional custom scopes to be added to auth token</param>
    /// <returns>Authentication data received by the identity provider</returns>
    Task<AuthenticationData> RequestClientCredentialsTokenAsync(ApiScopes apiScopes,
        DefaultScopes defaultScopes, IEnumerable<string>? customScopes = null);

    /// <summary>
    ///     Requests authentication using password and username.
    /// </summary>
    /// <param name="username">The Username</param>
    /// <param name="password">The password</param>
    /// <param name="apiScopes">The requested api scopes</param>
    /// <param name="customScopes">Optional additional custom scopes to be added to auth token</param>
    /// <returns>Authentication data received by the identity provider</returns>
    Task<AuthenticationData> RequestPasswordTokenAsync(string username, string password,
        ApiScopes apiScopes, IEnumerable<string>? customScopes = null);

    /// <summary>
    ///     Refreshes the token.
    /// </summary>
    /// <param name="refreshToken">The refresh token.</param>
    /// <returns>Authentication data received by the identity provider</returns>
    Task<AuthenticationData> RefreshTokenAsync(string refreshToken);
}