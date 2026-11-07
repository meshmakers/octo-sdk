using System.Net;
using System.Text;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects.ApiErrors;
using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.IdentityServices;

namespace Sdk.ServiceClient.Tests.IdentityServices;

/// <summary>
///     AB#8346 — GrantExternalTenantUser returns the stored mapping (201 body), the allow-list of AB#8345 is read and
///     replaced on the tenant-scoped route, If-Match is sent only when given, and problem answers stay readable through
///     <see cref="ServiceProblem" />. A local HTTP listener plays the identity service.
/// </summary>
public sealed class CrossTenantGrantClientTests : IDisposable
{
    private readonly HttpListener _listener;
    private readonly string _baseUri;
    private string? _method;
    private string? _pathAndQuery;
    private string? _requestBody;
    private string? _ifMatch;
    private int _statusCode = 200;
    private string _responseJson = "{}";

    public CrossTenantGrantClientTests()
    {
        const int maxAttempts = 50;
        var firstPort = 47000 + Random.Shared.Next(0, 2000);
        for (var attempt = 0;; attempt++)
        {
            var uri = $"http://127.0.0.1:{firstPort + attempt}/";
            var listener = new HttpListener();
            try
            {
                listener.Prefixes.Add(uri);
                listener.Start();
                _listener = listener;
                _baseUri = uri;
                break;
            }
            catch (HttpListenerException) when (attempt < maxAttempts - 1)
            {
                listener.Close();
            }
        }

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
            _ifMatch = context.Request.Headers["If-Match"];
            using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
            {
                _requestBody = await reader.ReadToEndAsync();
            }

            var bytes = Encoding.UTF8.GetBytes(_responseJson);
            context.Response.StatusCode = _statusCode;
            context.Response.ContentType = _statusCode >= 400 ? "application/problem+json" : "application/json";
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes);
            context.Response.Close();
        }
    }

    private IdentityServicesClient CreateClient(string tenantId = "customer") =>
        new(new IdentityServiceClientOptions { EndpointUri = _baseUri, TenantId = tenantId },
            A.Fake<IIdentityServiceClientAccessToken>());

    private static readonly CreateExternalTenantUserMappingDto Grant = new()
    {
        SourceTenantId = "meshmakers",
        SourceUserId = "686a6215fd3e5972ff5693fd",
        SourceUserName = "ignored-by-the-service",
        GroupIds = ["6a1b2c3d4e5f60718293a4b5", "6a1b2c3d4e5f60718293a4b6"]
    };

    [Fact]
    public async Task GrantExternalTenantUser_PostsGroupIds_AndReturnsTheStoredMapping()
    {
        _statusCode = 201;
        _responseJson = "{\"id\":\"6a1b2c3d4e5f60718293a4c0\",\"sourceTenantId\":\"meshmakers\"," +
                        "\"sourceUserId\":\"686a6215fd3e5972ff5693fd\",\"sourceUserName\":\"alice\"," +
                        "\"roleIds\":[],\"groupNames\":[\"Operators\",\"Viewers\"]}";

        var mapping = await CreateClient().GrantExternalTenantUser(Grant);

        Assert.Equal("POST", _method);
        Assert.EndsWith("/customer/v1/externalTenantUserMappings", _pathAndQuery);
        Assert.Contains("\"groupIds\":[\"6a1b2c3d4e5f60718293a4b5\",\"6a1b2c3d4e5f60718293a4b6\"]", _requestBody);
        Assert.Equal("alice", mapping.SourceUserName);
        Assert.Equal(["Operators", "Viewers"], mapping.GroupNames);
    }

    [Fact]
    public async Task GrantExternalTenantUser_UnknownGroup_ThrowsWithReadableProblem()
    {
        _statusCode = 400;
        _responseJson = "{\"status\":400,\"title\":\"Unknown group\",\"detail\":\"A group id is no group of this tenant.\"," +
                        "\"code\":\"UnknownGroup\",\"unknownGroupIds\":[\"6a1b2c3d4e5f60718293a4b6\"]}";

        var exception = await Assert.ThrowsAsync<ServiceClientResultException>(
            () => CreateClient().GrantExternalTenantUser(Grant));

        Assert.True(ServiceProblem.TryParse(exception, out var problem));
        Assert.Equal(CrossTenantGrantProblemCodes.UnknownGroup, problem.Code);
        Assert.Equal(["6a1b2c3d4e5f60718293a4b6"], problem.UnknownGroupIds);
        Assert.Equal(
            "Unknown group: A group id is no group of this tenant. (HTTP 400, code UnknownGroup). Unknown group ids: 6a1b2c3d4e5f60718293a4b6",
            problem.ToDisplayText());
    }

    [Fact]
    public async Task GrantExternalTenantUser_SuccessWithoutBody_Throws()
    {
        _statusCode = 201;
        _responseJson = "";

        await Assert.ThrowsAsync<ServiceClientResultException>(() => CreateClient().GrantExternalTenantUser(Grant));
    }

    [Fact]
    public async Task GetCrossTenantGrantPermissions_ReadsTheTenantScopedRoute()
    {
        _responseJson = "{\"targetTenantIds\":[\"customer-a\",\"customer-b\"]}";

        var permissions = await CreateClient("meshmakers").GetCrossTenantGrantPermissions();

        Assert.Equal("GET", _method);
        Assert.EndsWith("/meshmakers/v1/crossTenantGrantPermissions", _pathAndQuery);
        Assert.Equal(["customer-a", "customer-b"], permissions.TargetTenantIds);
    }

    [Fact]
    public async Task GetCrossTenantGrantPermissions_ForeignAdministrator_ThrowsForbidden()
    {
        _statusCode = 403;
        _responseJson = "";

        var exception = await Assert.ThrowsAsync<ServiceClientResultException>(
            () => CreateClient("meshmakers").GetCrossTenantGrantPermissions());

        Assert.Equal(HttpStatusCode.Forbidden, exception.HttpStatusCode);
    }

    [Fact]
    public async Task UpdateCrossTenantGrantPermissions_PutsTheWholeList_WithoutIfMatchByDefault()
    {
        _responseJson = "{\"targetTenantIds\":[\"customer-a\"]}";

        var stored = await CreateClient("meshmakers").UpdateCrossTenantGrantPermissions(
            new CrossTenantGrantPermissionsDto { TargetTenantIds = [" Customer-A "] });

        Assert.Equal("PUT", _method);
        Assert.EndsWith("/meshmakers/v1/crossTenantGrantPermissions", _pathAndQuery);
        Assert.Contains("\"targetTenantIds\":[\" Customer-A \"]", _requestBody);
        Assert.Null(_ifMatch);
        Assert.Equal(["customer-a"], stored.TargetTenantIds);
    }

    [Fact]
    public async Task UpdateCrossTenantGrantPermissions_EmptyList_IsSentAsEmptyArray()
    {
        _responseJson = "{\"targetTenantIds\":[]}";

        var stored = await CreateClient("meshmakers").UpdateCrossTenantGrantPermissions(
            new CrossTenantGrantPermissionsDto { TargetTenantIds = [] });

        Assert.Contains("\"targetTenantIds\":[]", _requestBody);
        Assert.Empty(stored.TargetTenantIds);
    }

    [Fact]
    public async Task UpdateCrossTenantGrantPermissions_SendsIfMatch_WhenGiven()
    {
        _responseJson = "{\"targetTenantIds\":[]}";

        await CreateClient("meshmakers").UpdateCrossTenantGrantPermissions(
            new CrossTenantGrantPermissionsDto(), "\"v7\"");

        Assert.Equal("\"v7\"", _ifMatch);
    }

    [Fact]
    public async Task UpdateCrossTenantGrantPermissions_ValidationProblem_Throws()
    {
        _statusCode = 400;
        _responseJson = "{\"status\":400,\"title\":\"One or more validation errors occurred.\"}";

        var exception = await Assert.ThrowsAsync<ServiceClientResultException>(() =>
            CreateClient("meshmakers").UpdateCrossTenantGrantPermissions(
                new CrossTenantGrantPermissionsDto { TargetTenantIds = ["meshmakers"] }));

        Assert.True(ServiceProblem.TryParse(exception, out var problem));
        Assert.Equal(400, problem.Status);
        Assert.Null(problem.Code);
    }

    [Fact]
    public async Task UpdateCrossTenantGrantPermissions_NullList_IsRefusedBeforeTheCall()
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() => CreateClient("meshmakers")
            .UpdateCrossTenantGrantPermissions(new CrossTenantGrantPermissionsDto { TargetTenantIds = null! }));

        Assert.Null(_method);
    }
}
