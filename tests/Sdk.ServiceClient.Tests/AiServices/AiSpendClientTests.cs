using System.Net;
using System.Text;
using Meshmakers.Octo.Sdk.ServiceClient.AiServices;

namespace Sdk.ServiceClient.Tests.AiServices;

/// <summary>
///     AB#8690 — the AI client reads the tenant's own spend under the tenant route and calls the cap administration under
///     <c>system/v1/aiSpendCaps</c> with the documented shape. A local HTTP listener plays the AI service.
/// </summary>
public sealed class AiSpendClientTests : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly string _baseUri;
    private string? _method;
    private string? _pathAndQuery;
    private string? _body;
    private string _responseJson = "{}";

    public AiSpendClientTests()
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

    private AiServicesClient CreateClient(string tenant = "customer") =>
        new(new AiServiceClientOptions { EndpointUri = _baseUri, TenantId = tenant },
            A.Fake<IAiServiceClientAccessToken>());

    private const string ReportJson =
        "{\"tenantId\":\"customer\",\"keySource\":\"CentralKey\",\"capMonthlyEur\":12.5," +
        "\"months\":[{\"month\":\"2026-11\",\"spentEur\":1.25,\"reservedEur\":0.5,\"callCount\":3," +
        "\"resetsAt\":\"2026-12-01T00:00:00+00:00\"}]}";

    [Fact]
    public async Task GetAiSpend_ReadsTheTenantRoute()
    {
        _responseJson = ReportJson;

        var report = await CreateClient().GetAiSpendAsync();

        Assert.Equal("GET", _method);
        Assert.Equal("/customer/v1/aiSpend", _pathAndQuery);
        Assert.Equal("CentralKey", report.KeySource);
        Assert.Equal(12.5m, report.CapMonthlyEur);
        Assert.Equal(3, Assert.Single(report.Months).CallCount);
    }

    [Fact]
    public async Task SetAiSpendCap_PutsToTheSystemRoute_WithCapKeySourceAndReason()
    {
        _responseJson = "{\"tenantId\":\"customer\",\"oldKeySource\":\"None\",\"oldCapMonthlyEur\":0," +
                        "\"newKeySource\":\"CentralKey\",\"newCapMonthlyEur\":20,\"changedBy\":\"user:alice\"," +
                        "\"changedAt\":\"2026-11-24T10:00:00+00:00\"}";

        var change = await CreateClient("octosystem").SetAiSpendCapAsync("customer",
            new SetAiSpendCapRequestDto { CapMonthlyEur = 20m, Reason = "pilot" });

        Assert.Equal("PUT", _method);
        Assert.Equal("/system/v1/aiSpendCaps/customer", _pathAndQuery);
        Assert.Contains("\"capMonthlyEur\":20", _body);
        Assert.Contains("\"keySource\":\"CentralKey\"", _body);
        Assert.Contains("\"reason\":\"pilot\"", _body);
        Assert.Equal(20m, change.NewCapMonthlyEur);
        Assert.Equal("None", change.OldKeySource);
    }

    [Fact]
    public async Task SetAiSpendCap_WithoutReason_IsRefusedBeforeAnyRequest()
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() => CreateClient("octosystem")
            .SetAiSpendCapAsync("customer", new SetAiSpendCapRequestDto { CapMonthlyEur = 1m }));
        Assert.Null(_method);
    }

    [Fact]
    public async Task GetAiSpendCap_AndAudit_UseTheSystemRoute()
    {
        _responseJson = ReportJson;
        await CreateClient("octosystem").GetAiSpendCapAsync("customer");
        Assert.Equal("/system/v1/aiSpendCaps/customer", _pathAndQuery);

        _responseJson = "[{\"at\":\"2026-11-24T10:00:00+00:00\",\"changedBy\":\"user:alice\",\"reason\":\"pilot\"," +
                        "\"oldKeySource\":\"None\",\"oldCapMonthlyEur\":0,\"newKeySource\":\"CentralKey\",\"newCapMonthlyEur\":20}]";
        var audit = await CreateClient("octosystem").GetAiSpendCapAuditAsync("customer", 10);
        Assert.Equal("/system/v1/aiSpendCaps/customer/audit?limit=10", _pathAndQuery);
        Assert.Equal("pilot", Assert.Single(audit).Reason);
    }

    [Fact]
    public async Task ListAiSpendCaps_UsesTheSystemRoute()
    {
        _responseJson = "[" + ReportJson + "]";

        var caps = await CreateClient("octosystem").ListAiSpendCapsAsync();

        Assert.Equal("/system/v1/aiSpendCaps", _pathAndQuery);
        Assert.Single(caps);
    }
}
