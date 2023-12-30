namespace Meshmakers.Octo.Sdk.ServiceClient.Authorization;

/// <summary>
/// Client for access the introspection endpoint of the authorization server.
/// </summary>
public interface IAuthorizationClient
{
    /// <summary>
    /// Introspect an API resource.
    /// </summary>
    /// <param name="accessToken">Access token to use for introspection.</param>
    /// <param name="apiName">Api name to introspect.</param>
    /// <param name="apiSecret">Api secret to use for introspection.</param>
    /// <returns></returns>
    Task<bool> IntrospectApiResource(string accessToken, string apiName, string apiSecret);

    /// <summary>
    /// Get user info from the authorization server.
    /// </summary>
    /// <param name="accessToken"></param>
    /// <returns></returns>
    Task<UserInfoData> GetUserInfoAsync(string accessToken);
}