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
    /// <param name="clientId">Optional client id override. Falls back to the configured <c>AuthenticatorOptions.ClientId</c> when null.</param>
    /// <param name="clientSecret">Optional client secret override. Falls back to the configured <c>AuthenticatorOptions.ClientSecret</c> when null.</param>
    /// <returns>Authentication data received by the identity provider</returns>
    Task<AuthenticationData> RequestClientCredentialsTokenAsync(ApiScopes apiScopes,
        DefaultScopes defaultScopes, IEnumerable<string>? customScopes = null,
        string? clientId = null, string? clientSecret = null);

    /// <summary>
    ///     Requests a client-credentials token for a named tenant (<c>acr_values=tenant:{tenantId}</c>) with the given
    ///     credentials, regardless of the tenant the client is configured for (AB#7192). The caller needs it to act in a
    ///     child tenant as the mirror of its own client: a token of the parent tenant is refused on a child's routes.
    /// </summary>
    /// <param name="tenantId">The tenant the token is issued for.</param>
    /// <param name="apiScopes">The requested api scopes</param>
    /// <param name="defaultScopes">The requested default scopes</param>
    /// <param name="clientId">Client id (the mirror of a client has the id of its parent client).</param>
    /// <param name="clientSecret">The client's secret in that tenant.</param>
    /// <returns>Authentication data received by the identity provider</returns>
    Task<AuthenticationData> RequestClientCredentialsTokenForTenantAsync(string tenantId, ApiScopes apiScopes,
        DefaultScopes defaultScopes, string clientId, string clientSecret);

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