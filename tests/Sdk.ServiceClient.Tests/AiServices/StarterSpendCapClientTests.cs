using System.Net;
using System.Text;
using Meshmakers.Octo.Sdk.ServiceClient.AiServices;

namespace Sdk.ServiceClient.Tests.AiServices;

/// <summary>
///     AB#9572 — the AI client applies the starter default cap with a body-less POST to
///     <c>system/v1/aiSpendCaps/{tenantId}/starterDefault</c> and reads the outcome. A local HTTP listener plays the AI
///     service.
/// </summary>
public sealed class StarterSpendCapClientTests : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly string _baseUri;
    private string? _method;
    private string? _pathAndQuery;
    private string? _body;
    private string _responseJson = "{}";

    public StarterSpendCapClientTests()
    {
        var started = false;
        var uri = "";
        for (var port = 47000 + Random.Shared.Next(0, 2000); !started; port++)
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
                _body = await reader.ReadToEndAsync();
            }

            var bytes = Encoding.UTF8.GetBytes(_responseJson);
            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes);
            context.Response.Close();
        }
    }

    private AiServicesClient CreateClient() =>
        new(new AiServiceClientOptions { EndpointUri = _baseUri, TenantId = "octosystem" },
            A.Fake<IAiServiceClientAccessToken>());

    [Fact]
    public async Task ApplyStarterDefault_PostsToTheSystemRoute_WithoutAmount()
    {
        _responseJson = "{\"tenantId\":\"starter1\",\"outcome\":\"created\",\"keySource\":\"CentralKey\"," +
                        "\"capMonthlyEur\":20,\"message\":\"ok\"}";

        var result = await CreateClient().ApplyStarterDefaultSpendCapAsync("starter1");

        Assert.Equal("POST", _method);
        Assert.Equal("/system/v1/aiSpendCaps/starter1/starterDefault", _pathAndQuery);
        Assert.DoesNotContain("cap", _body ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(StarterSpendCapResultDto.Created, result.Outcome);
        Assert.Equal("CentralKey", result.KeySource);
        Assert.Equal(20m, result.CapMonthlyEur);
    }

    [Fact]
    public async Task ApplyStarterDefault_ReadsExistsAndNotConfigured()
    {
        _responseJson = "{\"tenantId\":\"starter1\",\"outcome\":\"exists\",\"keySource\":\"None\",\"capMonthlyEur\":0}";
        Assert.Equal(StarterSpendCapResultDto.Exists,
            (await CreateClient().ApplyStarterDefaultSpendCapAsync("starter1")).Outcome);

        _responseJson = "{\"tenantId\":\"starter1\",\"outcome\":\"notConfigured\",\"keySource\":\"None\"}";
        Assert.Equal(StarterSpendCapResultDto.NotConfigured,
            (await CreateClient().ApplyStarterDefaultSpendCapAsync("starter1")).Outcome);
    }

    [Fact]
    public async Task ApplyStarterDefault_EscapesTheTenantId()
    {
        _responseJson = "{\"tenantId\":\"a b\",\"outcome\":\"notConfigured\",\"keySource\":\"None\"}";

        await CreateClient().ApplyStarterDefaultSpendCapAsync("a b");

        Assert.Equal("/system/v1/aiSpendCaps/a%20b/starterDefault", _pathAndQuery);
    }

    [Fact]
    public async Task ApplyStarterDefault_WithoutTenant_IsRefusedBeforeAnyRequest()
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() => CreateClient().ApplyStarterDefaultSpendCapAsync(" "));
        Assert.Null(_method);
    }
}
