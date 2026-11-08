using System.Net;
using System.Text;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects.ApiErrors;
using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.System;
using Meshmakers.Octo.Sdk.ServiceClient.Authentication;
using Sdk.ServiceClient.Tests.Authentication;

namespace Sdk.ServiceClient.Tests.AssetRepositoryServices.TenantScope;

/// <summary>
///     AB#6836: the choke point <c>CreateTenantScopeClient</c> never attaches a token that was issued for
///     another tenant. Observed on the wire, because the client builds its own <c>RestClient</c>.
/// </summary>
public sealed class TenantScopeTokenTests : IDisposable
{
    private const string LibraryStatusBody = """{"items":[]}""";
    private readonly WireService _service = new();

    public void Dispose() => _service.Dispose();

    [Fact]
    public async Task TokenOfTheAddressedTenant_IsSentWithoutExchange()
    {
        var token = TestJwt.For("child");
        var provider = new RecordingProvider();

        await NewClient(token, provider).GetLibraryStatusAsync("child");

        Assert.Equal(0, provider.Calls);
        Assert.Equal($"Bearer {token}", _service.Authorization.Single());
        Assert.Equal("GET /child/v1/models/LibraryStatus", _service.Requests.Single());
    }

    [Fact]
    public async Task TokenOfTheAddressedTenant_IsSentWithoutProviderToo()
    {
        var token = TestJwt.For("Child");

        await NewClient(token, null).GetLibraryStatusAsync("child");

        Assert.Equal($"Bearer {token}", _service.Authorization.Single());
    }

    [Fact]
    public async Task TokenOfAnotherTenant_IsExchanged_AndOnlyTheExchangedTokenIsSent()
    {
        var root = TestJwt.For("root", "root-token");
        var provider = new RecordingProvider();

        await NewClient(root, provider).GetLibraryStatusAsync("child");

        Assert.Equal(1, provider.Calls);
        Assert.Equal(root, provider.LastSubject);
        var sent = Assert.Single(_service.Authorization);
        Assert.NotEqual($"Bearer {root}", sent);
        Assert.Equal("child", AccessTokenInspector.TryGetTenantId(sent["Bearer ".Length..]));
    }

    [Fact]
    public async Task TokenOfAnotherTenant_WithoutProvider_IsRefused_AndNothingIsSent()
    {
        var root = TestJwt.For("root");

        var ex = await Assert.ThrowsAsync<TenantTokenMismatchException>(
            () => NewClient(root, null).ListBlueprintInstallationsAsync("child"));

        Assert.Equal("child", ex.TenantId);
        Assert.Empty(_service.Requests);
        Assert.DoesNotContain(root, ex.Message);
    }

    [Fact]
    public async Task ExchangeDenied_RaisesTheCause_AndNoRequestIsSent()
    {
        var root = TestJwt.For("root");
        var provider = new RecordingProvider { Deny = TenantTokenDenialCause.NotProvisioned };

        var ex = await Assert.ThrowsAsync<TenantTokenExchangeDeniedException>(
            () => NewClient(root, provider).GetLibraryStatusAsync("child"));

        Assert.Equal(TenantTokenDenialCause.NotProvisioned, ex.Cause);
        Assert.Empty(_service.Requests);
    }

    [Fact]
    public async Task EverySingleTenantMethod_GoesThroughTheGuard()
    {
        var root = TestJwt.For("root");
        var client = NewClient(root, null);
        var dto = new Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Blueprints.BlueprintUpdateRequestDto();

        await Assert.ThrowsAsync<TenantTokenMismatchException>(() => client.GetLibraryStatusAsync("child"));
        await Assert.ThrowsAsync<TenantTokenMismatchException>(() => client.ResolveDependenciesBatchAsync("child", []));
        await Assert.ThrowsAsync<TenantTokenMismatchException>(() => client.ApplyBlueprintAsync("child", "bp"));
        await Assert.ThrowsAsync<TenantTokenMismatchException>(() => client.GetBlueprintHistoryAsync("child"));
        await Assert.ThrowsAsync<TenantTokenMismatchException>(() => client.GetBlueprintUpdateInfoAsync("child"));
        await Assert.ThrowsAsync<TenantTokenMismatchException>(() => client.PreviewBlueprintUpdateAsync("child", dto));
        await Assert.ThrowsAsync<TenantTokenMismatchException>(() => client.ApplyBlueprintUpdateAsync("child", dto));
        await Assert.ThrowsAsync<TenantTokenMismatchException>(() => client.ListBlueprintInstallationsAsync("child"));
        await Assert.ThrowsAsync<TenantTokenMismatchException>(() => client.UninstallBlueprintAsync("child", "bp"));

        Assert.Empty(_service.Requests);
    }

    [Fact]
    public async Task SecondCallInTheSameSession_ReusesTheExchangedToken()
    {
        var root = TestJwt.For("root");
        var counting = new CountingExchange();
        var provider = new TenantTokenProvider(counting);
        var client = NewClient(root, provider);

        await client.GetLibraryStatusAsync("child");
        await client.GetLibraryStatusAsync("child");

        Assert.Equal(1, counting.Calls);
        Assert.Equal(2, _service.Authorization.Count);
        Assert.Equal(_service.Authorization[0], _service.Authorization[1]);
    }

    [Fact]
    public async Task Forbidden_OnATenantScopeCall_NamesTheTenantAndTheProvisioningStep()
    {
        _service.Status = 403;

        var ex = await Assert.ThrowsAsync<TenantTokenExchangeDeniedException>(
            () => NewClient(TestJwt.For("child"), null).ListBlueprintInstallationsAsync("child"));

        Assert.Equal(TenantTokenDenialCause.Forbidden, ex.Cause);
        Assert.Contains("ProvisionCurrentUser -ttid child", ex.Message);
    }

    /// <summary>
    ///     AB#8529: a 403 with a coded reason refuses the operation, not the tenant. The service's message and code
    ///     reach the caller (exception message and <see cref="ServiceProblem" />), not the provisioning hint.
    /// </summary>
    [Fact]
    public async Task Forbidden_WithACodedReason_SurfacesTheMessageAndTheCode()
    {
        _service.Status = 403;
        _service.Body =
            """{"message":"Access is denied: this operation requires the role 'DataModelManagement' in the tenant.","code":"DataModelManagementRequired"}""";

        var ex = await Assert.ThrowsAsync<ServiceClientResultException>(
            () => NewClient(TestJwt.For("child"), null).ApplyBlueprintAsync("child", "B-1.0.0", false));

        Assert.IsNotType<TenantTokenExchangeDeniedException>(ex);
        Assert.Equal(HttpStatusCode.Forbidden, ex.HttpStatusCode);
        Assert.Contains("DataModelManagement", ex.Message);
        Assert.Contains("DataModelManagementRequired", ex.Message);
        Assert.DoesNotContain("ProvisionCurrentUser", ex.Message);
        Assert.True(ServiceProblem.TryParse(ex, out var problem));
        Assert.Equal(AuthorizationProblemCodes.DataModelManagementRequired, problem.Code);
        Assert.Contains("requires the role 'DataModelManagement'", problem.Detail);
        Assert.Contains("code DataModelManagementRequired", problem.ToDisplayText());
    }

    [Theory]
    [InlineData("""{"message":"no role"}""")]
    [InlineData("not json")]
    [InlineData("""{"code":""}""")]
    public async Task Forbidden_WithoutACode_KeepsTheProvisioningHint(string body)
    {
        _service.Status = 403;
        _service.Body = body;

        var ex = await Assert.ThrowsAsync<TenantTokenExchangeDeniedException>(
            () => NewClient(TestJwt.For("child"), null).ListBlueprintInstallationsAsync("child"));

        Assert.Equal(TenantTokenDenialCause.Forbidden, ex.Cause);
    }

    [Theory]
    [InlineData("")]
    [InlineData("opaque-reference-token")]
    public async Task TokenWithoutReadableTenant_BehavesAsBefore(string token)
    {
        var provider = new RecordingProvider();

        await NewClient(token, provider).GetLibraryStatusAsync("child");

        Assert.Equal(0, provider.Calls);
        Assert.Equal(string.IsNullOrEmpty(token) ? 0 : 1, _service.Authorization.Count);
    }

    private AssetServicesClient NewClient(string token, ITenantTokenProvider? provider)
    {
        var accessToken = A.Fake<IAssetServiceClientAccessToken>();
        A.CallTo(() => accessToken.AccessToken).Returns(token);
        return new AssetServicesClient(new AssetServiceClientOptions { EndpointUri = _service.BaseUrl }, accessToken,
            provider);
    }

    private sealed class RecordingProvider : ITenantTokenProvider
    {
        public int Calls { get; private set; }
        public string? LastSubject { get; private set; }
        public TenantTokenDenialCause? Deny { get; set; }

        public Task<string> GetAccessTokenAsync(string targetTenantId, string subjectToken,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            LastSubject = subjectToken;
            if (Deny is { } cause)
            {
                throw new TenantTokenExchangeDeniedException(targetTenantId, cause);
            }

            return Task.FromResult(TestJwt.For(targetTenantId, "exchanged"));
        }
    }

    private sealed class CountingExchange : ITokenExchangeClient
    {
        public int Calls { get; private set; }

        public Task<AuthenticationData> ExchangeTokenForTenantAsync(string subjectToken, string targetTenantId,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(new AuthenticationData
            {
                AccessToken = TestJwt.For(targetTenantId, "exchanged"),
                ExpiresAt = DateTime.Now.AddHours(1)
            });
        }
    }

    private sealed class WireService : IDisposable
    {
        private readonly HttpListener _listener = new();

        public WireService()
        {
            using var socket = new global::System.Net.Sockets.Socket(
                global::System.Net.Sockets.AddressFamily.InterNetwork,
                global::System.Net.Sockets.SocketType.Stream, global::System.Net.Sockets.ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            BaseUrl = $"http://127.0.0.1:{((IPEndPoint)socket.LocalEndPoint!).Port}/";
            socket.Close();
            _listener.Prefixes.Add(BaseUrl);
            _listener.Start();
            _ = Task.Run(ServeAsync);
        }

        public string BaseUrl { get; }
        public List<string> Requests { get; } = [];
        public List<string> Authorization { get; } = [];
        public int Status { get; set; } = 200;
        public string Body { get; set; } = LibraryStatusBody;

        public void Dispose() => _listener.Abort();

        private async Task ServeAsync()
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

                lock (Requests)
                {
                    Requests.Add($"{context.Request.HttpMethod} {context.Request.Url?.AbsolutePath}");
                    var header = context.Request.Headers["Authorization"];
                    if (header != null)
                    {
                        Authorization.Add(header);
                    }
                }

                var bytes = Encoding.UTF8.GetBytes(Body);
                context.Response.StatusCode = Status;
                context.Response.ContentType = "application/json";
                context.Response.ContentLength64 = bytes.Length;
                await context.Response.OutputStream.WriteAsync(bytes);
                context.Response.Close();
            }
        }
    }
}
