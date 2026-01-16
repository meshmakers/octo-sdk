using System.Security.Claims;
using Meshmakers.Octo.Sdk.ServiceClient.Authorization;
using Microsoft.Extensions.Caching.Memory;

namespace Sdk.ServiceClient.Tests.Authorization;

public class UserInfoCacheTests
{
    private readonly IMemoryCache _memoryCache;
    private readonly IAuthorizationClient _authorizationClient;

    public UserInfoCacheTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _authorizationClient = A.Fake<IAuthorizationClient>();
    }

    [Fact]
    public async Task GetUserInfoAsync_FirstCall_CallsAuthorizationClient()
    {
        // Arrange
        const string accessToken = "test-token";
        var expectedUserInfo = new UserInfoData(true, new List<Claim> { new("sub", "user-123") });
        A.CallTo(() => _authorizationClient.GetUserInfoAsync(accessToken))
            .Returns(Task.FromResult(expectedUserInfo));

        var sut = new UserInfoCache(_memoryCache, _authorizationClient);

        // Act
        var result = await sut.GetUserInfoAsync(accessToken);

        // Assert
        Assert.Same(expectedUserInfo, result);
        A.CallTo(() => _authorizationClient.GetUserInfoAsync(accessToken))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetUserInfoAsync_SecondCallWithSameToken_ReturnsCachedValue()
    {
        // Arrange
        const string accessToken = "test-token";
        var expectedUserInfo = new UserInfoData(true, new List<Claim> { new("sub", "user-123") });
        A.CallTo(() => _authorizationClient.GetUserInfoAsync(accessToken))
            .Returns(Task.FromResult(expectedUserInfo));

        var sut = new UserInfoCache(_memoryCache, _authorizationClient);

        // Act
        var result1 = await sut.GetUserInfoAsync(accessToken);
        var result2 = await sut.GetUserInfoAsync(accessToken);

        // Assert
        Assert.Same(result1, result2);
        A.CallTo(() => _authorizationClient.GetUserInfoAsync(accessToken))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetUserInfoAsync_DifferentTokens_CallsAuthorizationClientForEach()
    {
        // Arrange
        const string accessToken1 = "test-token-1";
        const string accessToken2 = "test-token-2";

        var userInfo1 = new UserInfoData(true, new List<Claim> { new("sub", "user-1") });
        var userInfo2 = new UserInfoData(true, new List<Claim> { new("sub", "user-2") });

        A.CallTo(() => _authorizationClient.GetUserInfoAsync(accessToken1))
            .Returns(Task.FromResult(userInfo1));
        A.CallTo(() => _authorizationClient.GetUserInfoAsync(accessToken2))
            .Returns(Task.FromResult(userInfo2));

        var sut = new UserInfoCache(_memoryCache, _authorizationClient);

        // Act
        var result1 = await sut.GetUserInfoAsync(accessToken1);
        var result2 = await sut.GetUserInfoAsync(accessToken2);

        // Assert
        Assert.NotSame(result1, result2);
        Assert.Equal("user-1", result1.Claims?.First().Value);
        Assert.Equal("user-2", result2.Claims?.First().Value);
        A.CallTo(() => _authorizationClient.GetUserInfoAsync(A<string>._))
            .MustHaveHappenedTwiceExactly();
    }

    [Fact]
    public async Task GetUserInfoAsync_UnauthenticatedUser_CachesResult()
    {
        // Arrange
        const string accessToken = "invalid-token";
        var expectedUserInfo = new UserInfoData(false, null);
        A.CallTo(() => _authorizationClient.GetUserInfoAsync(accessToken))
            .Returns(Task.FromResult(expectedUserInfo));

        var sut = new UserInfoCache(_memoryCache, _authorizationClient);

        // Act
        var result1 = await sut.GetUserInfoAsync(accessToken);
        var result2 = await sut.GetUserInfoAsync(accessToken);

        // Assert
        Assert.False(result1.IsAuthenticated);
        Assert.Same(result1, result2);
        A.CallTo(() => _authorizationClient.GetUserInfoAsync(accessToken))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetUserInfoAsync_WithMultipleClaims_PreservesAllClaims()
    {
        // Arrange
        const string accessToken = "test-token";
        var claims = new List<Claim>
        {
            new("sub", "user-123"),
            new("email", "user@example.com"),
            new("role", "admin"),
            new("role", "user")
        };
        var expectedUserInfo = new UserInfoData(true, claims);
        A.CallTo(() => _authorizationClient.GetUserInfoAsync(accessToken))
            .Returns(Task.FromResult(expectedUserInfo));

        var sut = new UserInfoCache(_memoryCache, _authorizationClient);

        // Act
        var result = await sut.GetUserInfoAsync(accessToken);

        // Assert
        Assert.True(result.IsAuthenticated);
        Assert.NotNull(result.Claims);
        Assert.Equal(4, result.Claims.Count());
    }
}
