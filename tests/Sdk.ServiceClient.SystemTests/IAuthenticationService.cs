using Meshmakers.Octo.Sdk.ServiceClient;

namespace FireGuardians.Services;

/// <summary>
///     Service for interacting with the authentication
/// </summary>
internal interface IAuthenticationService
{
    /// <summary>
    ///     Ensures that the token is set and still valid.
    /// </summary>
    /// <param name="serviceClientAccessToken"></param>
    /// <returns></returns>
    Task EnsureAuthenticated(IServiceClientAccessToken serviceClientAccessToken);
}