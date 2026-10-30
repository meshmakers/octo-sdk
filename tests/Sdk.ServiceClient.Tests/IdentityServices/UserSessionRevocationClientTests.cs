using System.Net;
using System.Text;
using Meshmakers.Octo.Sdk.ServiceClient.IdentityServices;

namespace Sdk.ServiceClient.Tests.IdentityServices;

/// <summary>
///     AB#7899 — the identity client calls the revocation endpoints of the client's tenant with the documented shape
///     and reads their answers. A local HTTP listener plays the identity service.
/// </summary>
public sealed class UserSessionRevocationClientTests : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly string _baseUri;
    private string? _method;
    private string? _pathAndQuery;
    private string _responseJson = "{}";

    public UserSessionRevocationClientTests()
    {
        var started = false;
        var uri = "";
        for (var port = 43000 + Random.Shared.Next(0, 2000); !started; port++)
        {
            try
            {
                uri = $"http://127.0.0.1:{port}/";
                _listener.Prefixes.Clear();
                _listener.Prefixes.Add(uri);
                _listener.Start();
                started = true;
            }
            catch (HttpListenerException)
            {
                // port taken, try the next one
            }
        }

        _baseUri = uri;
        _ = Task.Run(Serve);
    }

    public void Dispose() => _listener.Close();

    private async Task Serve()
    {
        while (_listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (Exception)
            {
                return;
            }

            _method = context.Request.HttpMethod;
            _pathAndQuery = context.Request.Url?.PathAndQuery;
            var bytes = Encoding.UTF8.GetBytes(_responseJson);
            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes);
            context.Response.Close();
        }
    }

    private IdentityServicesClient CreateClient() =>
        new(new IdentityServiceClientOptions { EndpointUri = _baseUri, TenantId = "customer" },
            A.Fake<IIdentityServiceClientAccessToken>());

    [Fact]
    public async Task RevokeUserSessions_PostsToTheUsersRoute_AndReadsTheCounts()
    {
        _responseJson = "{\"userName\":\"alice\",\"subject\":\"u1\",\"securityStampRotated\":true," +
                        "\"revokedTokens\":3,\"revokedAuthorizations\":1,\"removedSessions\":2}";

        var result = await CreateClient().RevokeUserSessions("alice");

        Assert.Equal("POST", _method);
        Assert.EndsWith("/customer/v1/users/alice/revokeSessions", _pathAndQuery);
        Assert.Equal("alice", result.UserName);
        Assert.True(result.SecurityStampRotated);
        Assert.Equal(3, result.RevokedTokens);
        Assert.Equal(1, result.RevokedAuthorizations);
        Assert.Equal(2, result.RemovedSessions);
    }

    [Fact]
    public async Task RevokeUserSessions_SuccessWithoutBody_IsAnError_NotANullResult()
    {
        _responseJson = "";

        await Assert.ThrowsAsync<Meshmakers.Octo.Sdk.ServiceClient.ServiceClientResultException>(
            () => CreateClient().RevokeUserSessions("alice"));
    }

    [Theory]
    [InlineData(true, "dryRun=true")]
    [InlineData(false, "dryRun=false")]
    public async Task RevokeShadowUserSessions_SendsScopeAndDryRun_AndReadsTheSummary(bool dryRun, string expectedQuery)
    {
        _responseJson = "{\"dryRun\":" + (dryRun ? "true" : "false") + ",\"scope\":\"shadowUsers\",\"tenantId\":\"customer\"," +
                        "\"users\":2,\"tokens\":5,\"authorizations\":1,\"sessions\":4,\"userNames\":[\"xt_a_b\",\"xt_a_c\"]}";

        var result = await CreateClient().RevokeShadowUserSessions(dryRun);

        Assert.Equal("POST", _method);
        Assert.Contains("/customer/v1/users/revokeSessions?", _pathAndQuery);
        Assert.Contains("scope=shadowUsers", _pathAndQuery);
        Assert.Contains(expectedQuery, _pathAndQuery);
        Assert.Equal(dryRun, result.DryRun);
        Assert.Equal(2, result.Users);
        Assert.Equal(5, result.Tokens);
        Assert.Equal(4, result.Sessions);
        Assert.Equal(["xt_a_b", "xt_a_c"], result.UserNames);
    }
}
