using System.Text.Json;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.StreamData;
using Sdk.ServiceClient.Tests.BotServices;

namespace Sdk.ServiceClient.Tests.AssetRepositoryServices.StreamData;

/// <summary>
///     AB#7596 — the SDK write method for time-range archives, pinned on the wire against a loopback service: route,
///     the origin by name (default automated) and the optional reason of a manual write.
/// </summary>
public class StreamDataInsertTimeRangeClientTests : IClassFixture<LoopbackHttpService>
{
    private const string Path = "/acme/v1/streamdata/archives/6700000000000000000000b1/insertTimeRange";
    private readonly LoopbackHttpService _service;

    public StreamDataInsertTimeRangeClientTests(LoopbackHttpService service)
    {
        _service = service;
        _service.Reset();
    }

    private static TimeRangePointDto Point(StreamDataOrigin? origin = null, string? reason = null) => new()
    {
        RtId = "6700000000000000000000c1",
        CkTypeId = "Basic.Energy/EnergyMeasurement",
        From = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
        To = new DateTime(2026, 3, 1, 0, 15, 0, DateTimeKind.Utc),
        Attributes = new Dictionary<string, object?> { ["Amount.Value"] = 1.5 },
        Origin = origin ?? StreamDataOrigin.Automated,
        Reason = reason
    };

    [Fact]
    public async Task InsertTimeRangeAsync_WithoutOrigin_PostsAnAutomatedPoint()
    {
        _service.RespondWith(Path, 204, string.Empty);

        await CreateClient().InsertTimeRangeAsync("acme", "6700000000000000000000b1", [Point()]);

        Assert.Equal("POST " + Path, _service.SingleRequest());
        var point = JsonDocument.Parse(_service.Bodies.Single()).RootElement[0];
        Assert.Equal("automated", point.GetProperty("origin").GetString());
        Assert.Equal(JsonValueKind.Null, point.GetProperty("reason").ValueKind);
        Assert.Equal(1.5, point.GetProperty("attributes").GetProperty("Amount.Value").GetDouble());
    }

    [Fact]
    public async Task InsertTimeRangeAsync_ManualWithReason_SendsBothAndNoWriter()
    {
        _service.RespondWith(Path, 204, string.Empty);

        await CreateClient().InsertTimeRangeAsync("acme", "6700000000000000000000b1",
            [Point(StreamDataOrigin.Manual, "meter exchanged")]);

        var point = JsonDocument.Parse(_service.Bodies.Single()).RootElement[0];
        Assert.Equal("manual", point.GetProperty("origin").GetString());
        Assert.Equal("meter exchanged", point.GetProperty("reason").GetString());
        // The writer is the caller's token, recorded by the service; the SDK never sends one.
        Assert.False(point.TryGetProperty("writtenBy", out _));
    }

    [Fact]
    public async Task InsertTimeRangeAsync_EmptyBatch_SendsNothing()
    {
        await CreateClient().InsertTimeRangeAsync("acme", "6700000000000000000000b1", []);

        Assert.Empty(_service.Requests);
    }

    [Fact]
    public async Task InsertTimeRangeAsync_Refused_ThrowsWithTheStatus()
    {
        _service.RespondWith(Path, 400, "\"Point 0: origin 'x' is not valid.\"");

        await Assert.ThrowsAnyAsync<Exception>(() =>
            CreateClient().InsertTimeRangeAsync("acme", "6700000000000000000000b1", [Point()]));
    }

    [Fact]
    public async Task InsertTimeRangeAsync_ReadsTheWriteResult()
    {
        _service.RespondWith(Path, 200, """
            {"stored":1,"skipped":1,"skippedByReason":{"ManualValueProtected":1},
             "skippedRows":[{"rtId":"6700000000000000000000c1","from":"2026-03-01T00:00:00Z","to":"2026-03-01T00:15:00Z",
                             "origin":"automated","reason":"ManualValueProtected"}]}
            """);

        var result = await CreateClient().InsertTimeRangeAsync("acme", "6700000000000000000000b1", [Point(), Point()]);

        Assert.Equal(1, result.Stored);
        Assert.Equal(1, result.Skipped);
        Assert.Equal(1, result.SkippedByReason["ManualValueProtected"]);
        var row = Assert.Single(result.SkippedRows);
        Assert.Equal("ManualValueProtected", row.Reason);
        Assert.Equal(new DateTime(2026, 3, 1, 0, 15, 0, DateTimeKind.Utc), row.To!.Value.ToUniversalTime());
    }

    [Fact]
    public async Task InsertTimeRangeAsync_AnOlderServiceWithoutBody_CountsEveryRowAsHandedOver()
    {
        _service.RespondWith(Path, 204, string.Empty);

        var result = await CreateClient().InsertTimeRangeAsync("acme", "6700000000000000000000b1", [Point(), Point()]);

        Assert.Equal(2, result.Stored);
        Assert.Equal(0, result.Skipped);
    }

    private StreamDataServicesClient CreateClient()
    {
        var options = new StreamDataServiceClientOptions { EndpointUri = _service.BaseUrl, TenantId = "acme" };
        return new StreamDataServicesClient(options, A.Fake<IStreamDataServiceClientAccessToken>());
    }
}
