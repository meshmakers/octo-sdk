namespace Meshmakers.Octo.Sdk.ServiceClient.Authorization;

/// <summary>
/// Interface to access optionally cached user info from the authorization server.
/// </summary>
public interface IUserInfoCache
{
    /// <summary>
    ///     Get (cached) user info from the authorization server.
    /// </summary>
    /// <param name="accessToken">The access token to use for the user info request.</param>
    /// <returns></returns>
    Task<UserInfoData> GetUserInfoAsync(string accessToken);
}