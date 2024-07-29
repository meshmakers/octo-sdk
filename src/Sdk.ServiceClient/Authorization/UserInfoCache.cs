using Microsoft.Extensions.Caching.Memory;

namespace Meshmakers.Octo.Sdk.ServiceClient.Authorization;

/// <summary>
/// Implementation of the user info cache.
/// </summary>
public class UserInfoCache : IUserInfoCache
{
    private const int CacheDurationMinutes = 5;
    private readonly IMemoryCache _cache;
    private readonly IAuthorizationClient _authorizationClient;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(CacheDurationMinutes);

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="cache">Memory cache</param>
    /// <param name="authorizationClient">Authorization client</param>
    public UserInfoCache(IMemoryCache cache, IAuthorizationClient authorizationClient)
    {
        _cache = cache;
        _authorizationClient = authorizationClient;
    }

    /// <inheritdoc />
    public async Task<UserInfoData> GetUserInfoAsync(string accessToken)
    {
        if (_cache.TryGetValue(accessToken, out UserInfoData? userInfo) && userInfo != null)
        {
            return userInfo;
        }

        userInfo =  await _authorizationClient.GetUserInfoAsync(accessToken);
        _cache.Set(accessToken, userInfo, CacheDuration);

        return userInfo;
    }
}