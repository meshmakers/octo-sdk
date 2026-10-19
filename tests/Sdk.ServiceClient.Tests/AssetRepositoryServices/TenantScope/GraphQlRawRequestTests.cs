using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.System;
using Meshmakers.Octo.Sdk.ServiceClient.Authentication;
using Sdk.ServiceClient.Tests.Authentication;

namespace Sdk.ServiceClient.Tests.AssetRepositoryServices.TenantScope;

/// <summary>
///     AB#7027: <see cref="IAssetServicesClient.SendGraphQlAsync" /> posts to <c>tenants/{tenant}/GraphQL</c>, hands
///     the answer back untouched and takes its token from the same choke point as the REST routes (AB#6836).
///     Observed on the wire against a loopback listener.
/// </summary>
public sealed class GraphQlRawRequestTests : IClassFixture<GraphQlRawRequestTests.WireService>
{
    private const string Canary = "CANARY-ROOT-TOKEN-7027";
    private readonly WireService _service;

    // One listener per class: registering a prefix costs ~5 s per instance on macOS.
    public GraphQlRawRequestTests(WireService service)
    {
        _service = service;
        _service.Reset();
    }

    [Fact]
    public async Task PostsToTheGraphQlRouteOfTheTenant_WithQueryVariablesAndOperationName()
    {
        var token = TestJwt.For("child");

        await NewClient(token, null).SendGraphQlAsync("child", "query Q($a: Int) { x(a: $a) }", """{"a":1}""", "Q", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("POST /tenants/child/GraphQL", _service.Requests.Single());
        using var body = JsonDocument.Parse(_service.Bodies.Single());
        Assert.Equal("query Q($a: Int) { x(a: $a) }", body.RootElement.GetProperty("query").GetString());
        Assert.Equal(1, body.RootElement.GetProperty("variables").GetProperty("a").GetInt32());
        Assert.Equal("Q", body.RootElement.GetProperty("operationName").GetString());
        Assert.StartsWith("application/json", _service.ContentTypes.Single());
    }

    [Fact]
    public async Task WithoutVariablesAndOperationName_OnlyTheQueryIsSent()
    {
        await NewClient(TestJwt.For("child"), null).SendGraphQlAsync("child", "{ x }", cancellationToken: TestContext.Current.CancellationToken);

        using var body = JsonDocument.Parse(_service.Bodies.Single());
        Assert.Equal(["query"], body.RootElement.EnumerateObject().Select(p => p.Name).ToArray());
    }

    [Fact]
    public async Task ResponseBody_IsReturnedUntouched()
    {
        // Odd spacing and key order must survive: the caller gets the bytes the service sent.
        _service.Body = """{ "data" :{"b":2,  "a":[1,2,3]} }""";

        var response = await NewClient(TestJwt.For("child"), null).SendGraphQlAsync("child", "{ x }", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("""{ "data" :{"b":2,  "a":[1,2,3]} }""", response.Content);
        Assert.False(response.HasErrors);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData(200)]
    [InlineData(400)]
    public async Task GraphQlErrors_AreAnAnswer_NotAnException(int status)
    {
        _service.Status = status;
        _service.Body = """{"errors":[{"message":"Cannot query field \"nope\""}]}""";

        var response = await NewClient(TestJwt.For("child"), null).SendGraphQlAsync("child", "{ nope }", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(response.HasErrors);
        Assert.Equal((HttpStatusCode)status, response.StatusCode);
        Assert.Contains("nope", response.Content);
    }

    [Fact]
    public async Task EmptyErrorsArray_IsNoError()
    {
        _service.Body = """{"data":{},"errors":[]}""";

        var response = await NewClient(TestJwt.For("child"), null).SendGraphQlAsync("child", "{ x }", cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(response.HasErrors);
    }

    [Fact]
    public async Task ServerError_WithoutGraphQlBody_Throws()
    {
        _service.Status = 502;
        _service.Body = "bad gateway";

        var ex = await Assert.ThrowsAsync<ServiceClientResultException>(
            () => NewClient(TestJwt.For("child"), null).SendGraphQlAsync("child", "{ x }", cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.BadGateway, ex.HttpStatusCode);
    }

    [Fact]
    public async Task Unauthorized_IsReported_EvenWithAnErrorsBody()
    {
        _service.Status = 401;
        _service.Body = """{"errors":[{"message":"no"}]}""";

        await Assert.ThrowsAsync<UnauthorizedServiceAccessException>(
            () => NewClient(TestJwt.For("child"), null).SendGraphQlAsync("child", "{ x }", cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Forbidden_NamesTheTenantAndTheProvisioningStep()
    {
        _service.Status = 403;

        var ex = await Assert.ThrowsAsync<TenantTokenExchangeDeniedException>(
            () => NewClient(TestJwt.For("child"), null).SendGraphQlAsync("child", "{ x }", cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(TenantTokenDenialCause.Forbidden, ex.Cause);
        Assert.Contains("ProvisionCurrentUser -ttid child", ex.Message);
    }

    [Fact]
    public async Task TokenOfAnotherTenant_IsExchanged_AndOnlyTheTenantTokenReachesTheGraphQlRoute()
    {
        var root = TestJwt.For("root", Canary);
        var provider = new RecordingProvider();

        await NewClient(root, provider).SendGraphQlAsync("child", "{ x }", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(1, provider.Calls);
        Assert.Equal(root, provider.LastSubject);
        var sent = Assert.Single(_service.Authorization);
        Assert.NotEqual($"Bearer {root}", sent);
        Assert.Equal("child", AccessTokenInspector.TryGetTenantId(sent["Bearer ".Length..]));
        Assert.Equal("POST /tenants/child/GraphQL", _service.Requests.Single());
    }

    [Fact]
    public async Task TokenOfAnotherTenant_WithoutProvider_IsRefused_NothingIsSent_AndTheTokenIsNotInTheMessage()
    {
        var root = TestJwt.For("root", Canary);

        var ex = await Assert.ThrowsAsync<TenantTokenMismatchException>(
            () => NewClient(root, null).SendGraphQlAsync("child", "{ x }", cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal("child", ex.TenantId);
        Assert.Empty(_service.Requests);
        Assert.DoesNotContain(Canary, ex.ToString());
        Assert.DoesNotContain(root, ex.ToString());
    }

    [Theory]
    [InlineData(TenantTokenDenialCause.NotProvisioned)]
    [InlineData(TenantTokenDenialCause.AccessToTenantDenied)]
    [InlineData(TenantTokenDenialCause.ClientNotAllowed)]
    [InlineData(TenantTokenDenialCause.SubjectTokenRejected)]
    public async Task ExchangeDenied_RaisesTheCause_NothingIsSent_AndTheTokenIsNotInTheMessage(
        TenantTokenDenialCause cause)
    {
        var root = TestJwt.For("root", Canary);
        var provider = new RecordingProvider { Deny = cause };

        var ex = await Assert.ThrowsAsync<TenantTokenExchangeDeniedException>(
            () => NewClient(root, provider).SendGraphQlAsync("child", "{ x }", cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(cause, ex.Cause);
        Assert.Empty(_service.Requests);
        Assert.DoesNotContain(Canary, ex.ToString());
        Assert.DoesNotContain(root, ex.ToString());
    }

    [Fact]
    public async Task SecondCall_ReusesTheExchangedToken()
    {
        var counting = new CountingExchange();
        var client = NewClient(TestJwt.For("root"), new TenantTokenProvider(counting));

        await client.SendGraphQlAsync("child", "{ x }", cancellationToken: TestContext.Current.CancellationToken);
        await client.SendGraphQlAsync("child", "{ x }", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(1, counting.Calls);
        Assert.Equal(_service.Authorization[0], _service.Authorization[1]);
    }

    [Fact]
    public async Task TheGuardCoversTheGraphQlRoute_LikeEveryOtherTenantMethod()
    {
        var client = NewClient(TestJwt.For("root"), null);

        await Assert.ThrowsAsync<TenantTokenMismatchException>(() => client.SendGraphQlAsync("child", "{ x }", cancellationToken: TestContext.Current.CancellationToken));

        Assert.Empty(_service.Requests);
    }

    [Theory]
    [InlineData("[1,2]")]
    [InlineData("\"text\"")]
    [InlineData("{ not json")]
    public async Task VariablesThatAreNoJsonObject_AreRejected_BeforeAnythingIsSent(string variables)
    {
        var client = NewClient(TestJwt.For("child"), null);

        await Assert.ThrowsAsync<ArgumentException>(() => client.SendGraphQlAsync("child", "{ x }", variables, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Empty(_service.Requests);
    }

    [Fact]
    public async Task TenantIdIsEscapedInTheRoute()
    {
        await NewClient(TestJwt.For(null), null).SendGraphQlAsync("a/../b", "{ x }", cancellationToken: TestContext.Current.CancellationToken);

        Assert.DoesNotContain("/../", _service.Requests.Single());
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

    public sealed class WireService : IDisposable
    {
        private readonly HttpListener _listener = new();

        public WireService()
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
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
        public List<string> Bodies { get; } = [];
        public List<string> ContentTypes { get; } = [];
        public int Status { get; set; } = 200;
        public string Body { get; set; } = """{"data":{"x":1}}""";

        public void Reset()
        {
            lock (Requests)
            {
                Requests.Clear();
                Bodies.Clear();
                ContentTypes.Clear();
                Authorization.Clear();
            }

            Status = 200;
            Body = """{"data":{"x":1}}""";
        }

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

                string requestBody;
                using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
                {
                    requestBody = await reader.ReadToEndAsync();
                }

                lock (Requests)
                {
                    Requests.Add($"{context.Request.HttpMethod} {context.Request.Url?.AbsolutePath}");
                    Bodies.Add(requestBody);
                    ContentTypes.Add(context.Request.ContentType ?? string.Empty);
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
