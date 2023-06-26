using System.Threading.Tasks;
using Meshmakers.Octo.Common.Shared;
using Meshmakers.Octo.Common.Shared.Authorization;

namespace Meshmakers.Octo.Sdk.ServiceClient.Authentication;

/// <summary>
/// Interface for the authenticator client.
/// </summary>
public interface IAuthenticatorClient : IAuthorizationClient
{
    /// <summary>
    /// Ensures that the user is authenticated, if not it will try to refresh the token.
    /// </summary>
    /// <param name="refreshToken">The refresh token.</param>
    /// <param name="accessToken">The current access token</param>
    /// <returns>Authentication data received by the identity provider</returns>
    Task<EnsureAuthenticatedData> EnsureAuthenticatedAsync(string refreshToken, string accessToken);

    /// <summary>
    /// Requests authentication using device authorization.
    /// </summary>
    /// <param name="apiScopes">The requested api scopes</param>
    /// <returns></returns>
    Task<DeviceAuthenticationRequestData> RequestDeviceAuthorizationAsync(CommonConstants.ApiScopes apiScopes);

    /// <summary>
    /// Requests authentication using a device code received by <seealso cref="RequestDeviceAuthorizationAsync"/>
    /// </summary>
    /// <param name="deviceCode">The device code</param>
    /// <returns>Authentication data received by the identity provider</returns>
    Task<DeviceAuthenticationData> RequestDeviceTokenAsync(string deviceCode);

    /// <summary>
    /// Requests authentication using client credentials.
    /// </summary>
    /// <param name="apiScopes">The requested api scopes</param>
    /// <param name="defaultScopes">The requested default scopes</param>
    /// <returns>Authentication data received by the identity provider</returns>
    Task<AuthenticationData> RequestClientCredentialsTokenAsync(CommonConstants.ApiScopes apiScopes,
        CommonConstants.DefaultScopes defaultScopes);

    /// <summary>
    /// Requests authentication using password and username.
    /// </summary>
    /// <param name="username">The Username</param>
    /// <param name="password">The password</param>
    /// <param name="apiScopes">The requested api scopes</param>
    /// <returns>Authentication data received by the identity provider</returns>
    Task<AuthenticationData> RequestPasswordTokenAsync(string username, string password,
        CommonConstants.ApiScopes apiScopes);

    /// <summary>
    /// Refreshes the token.
    /// </summary>
    /// <param name="refreshToken">The refresh token.</param>
    /// <returns>Authentication data received by the identity provider</returns>
    Task<AuthenticationData> RefreshTokenAsync(string refreshToken);
}
