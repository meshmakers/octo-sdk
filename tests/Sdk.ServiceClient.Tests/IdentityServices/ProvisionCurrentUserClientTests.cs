using System.Net;
using System.Text;
using Meshmakers.Octo.Sdk.ServiceClient.IdentityServices;

namespace Sdk.ServiceClient.Tests.IdentityServices;

/// <summary>
///     AB#8017 — ProvisionCurrentUser posts to the admin-provisioning route and returns the mapping the identity
///     service wrote, so callers can name the source identity (the home identity when the caller was a shadow user).
///     A local HTTP listener plays the identity service.
/// </summary>
public sealed class ProvisionCurrentUserClientTests : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly string _baseUri;
    private string? _method;
    private string? _pathAndQuery;
    private int _statusCode = 201;
    private string _responseJson = "{}";

    public ProvisionCurrentUserClientTests()
    {
        var started = false;
        var uri = "";
        for (var port = 45000 + Random.Shared.Next(0, 2000); !started; port++)
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
            context.Response.StatusCode = _statusCode;
            context.Response.ContentType = "application/json";
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes);
            context.Response.Close();
        }
    }

    private IdentityServicesClient CreateClient() =>
        new(new IdentityServiceClientOptions { EndpointUri = _baseUri, TenantId = "octosystem" },
            A.Fake<IIdentityServiceClientAccessToken>());

    [Theory]
    [InlineData(201)]
    [InlineData(200)]
    public async Task ProvisionCurrentUser_PostsToTheProvisioningRoute_AndReturnsTheWrittenMapping(int statusCode)
    {
        _statusCode = statusCode;
        _responseJson = "{\"id\":\"6a1b2c3d4e5f60718293a4b5\",\"sourceTenantId\":\"octosystem\"," +
                        "\"sourceUserId\":\"686a6215fd3e5972ff5693fd\",\"sourceUserName\":\"alice\"," +
                        "\"roleIds\":[\"r1\"],\"groupNames\":[\"TenantOwners\"]}";

        var mapping = await CreateClient().ProvisionCurrentUser("grandchild");

        Assert.Equal("POST", _method);
        Assert.EndsWith("/octosystem/v1/adminProvisioning/grandchild/provisionCurrentUser", _pathAndQuery);
        Assert.NotNull(mapping);
        Assert.Equal("octosystem", mapping.SourceTenantId);
        Assert.Equal("686a6215fd3e5972ff5693fd", mapping.SourceUserId);
        Assert.Equal("alice", mapping.SourceUserName);
        Assert.Contains("TenantOwners", mapping.GroupNames);
    }
}
