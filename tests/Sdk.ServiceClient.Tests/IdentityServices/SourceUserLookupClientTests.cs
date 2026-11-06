using System.Net;
using System.Text;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.IdentityServices;

namespace Sdk.ServiceClient.Tests.IdentityServices;

/// <summary>
///     AB#8340 — LookupSourceUser posts the identifier in the body (never in the URL) to the tenant-scoped lookup route
///     and maps the uniform <c>404 SourceUserNotFound</c> to <c>null</c>; every other failure throws. A local HTTP
///     listener plays the identity service.
/// </summary>
public sealed class SourceUserLookupClientTests : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly string _baseUri;
    private string? _method;
    private string? _pathAndQuery;
    private string? _requestBody;
    private int _statusCode = 200;
    private string _responseJson = "{}";

    public SourceUserLookupClientTests()
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
            using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
            {
                _requestBody = await reader.ReadToEndAsync();
            }

            var bytes = Encoding.UTF8.GetBytes(_responseJson);
            context.Response.StatusCode = _statusCode;
            context.Response.ContentType = "application/json";
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes);
            context.Response.Close();
        }
    }

    private IdentityServicesClient CreateClient() =>
        new(new IdentityServiceClientOptions { EndpointUri = _baseUri, TenantId = "customer" },
            A.Fake<IIdentityServiceClientAccessToken>());

    private static readonly SourceUserLookupRequestDto Request =
        new() { SourceTenantId = "meshmakers", Identifier = "alice@example.io" };

    [Fact]
    public async Task LookupSourceUser_PostsTheIdentifierInTheBody_AndReturnsTheUser()
    {
        _responseJson = "{\"sourceTenantId\":\"meshmakers\",\"userId\":\"686a6215fd3e5972ff5693fd\"," +
                        "\"userName\":\"alice\",\"displayName\":\"Alice A.\"}";

        var user = await CreateClient().LookupSourceUser(Request);

        Assert.Equal("POST", _method);
        Assert.EndsWith("/customer/v1/externalTenantUserMappings/sourceUserLookup", _pathAndQuery);
        Assert.DoesNotContain("alice", _pathAndQuery);
        Assert.Contains("\"identifier\":\"alice@example.io\"", _requestBody);
        Assert.Contains("\"sourceTenantId\":\"meshmakers\"", _requestBody);
        Assert.NotNull(user);
        Assert.Equal("686a6215fd3e5972ff5693fd", user.UserId);
        Assert.Equal("alice", user.UserName);
        Assert.Equal("Alice A.", user.DisplayName);
    }

    [Fact]
    public async Task LookupSourceUser_UniformNotFound_ReturnsNull()
    {
        _statusCode = 404;
        _responseJson = "{\"status\":404,\"title\":\"Source user not found\",\"code\":\"SourceUserNotFound\"}";

        var user = await CreateClient().LookupSourceUser(Request);

        Assert.Null(user);
    }

    [Theory]
    [InlineData(404, "")]
    [InlineData(404, "{\"status\":404,\"title\":\"Not Found\"}")]
    [InlineData(429, "{\"status\":429,\"code\":\"SourceUserLookupRateLimited\"}")]
    [InlineData(403, "{\"status\":403}")]
    public async Task LookupSourceUser_OtherFailures_Throw(int statusCode, string responseJson)
    {
        _statusCode = statusCode;
        _responseJson = responseJson;

        var exception = await Assert.ThrowsAsync<ServiceClientResultException>(
            () => CreateClient().LookupSourceUser(Request));

        Assert.Equal(statusCode, (int)exception.HttpStatusCode);
    }

    [Theory]
    [InlineData("", "alice")]
    [InlineData("meshmakers", "")]
    [InlineData("meshmakers", "   ")]
    public async Task LookupSourceUser_EmptyArguments_AreRefusedBeforeTheCall(string sourceTenantId, string identifier)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() => CreateClient().LookupSourceUser(
            new SourceUserLookupRequestDto { SourceTenantId = sourceTenantId, Identifier = identifier }));

        Assert.Null(_method);
    }
}
