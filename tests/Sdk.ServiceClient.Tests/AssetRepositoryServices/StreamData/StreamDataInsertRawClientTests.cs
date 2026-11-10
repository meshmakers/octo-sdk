using System.Text.Json;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.StreamData;
using Sdk.ServiceClient.Tests.BotServices;

namespace Sdk.ServiceClient.Tests.AssetRepositoryServices.StreamData;

/// <summary>
///     AB#8590 — the SDK write method for raw archives, pinned on the wire against a loopback service: route, timestamp
///     in UTC, origin by name (default automated), the reason of a manual write, explicit nulls kept, the write result
///     read and an unreadable answer never reported as stored.
/// </summary>
public class StreamDataInsertRawClientTests : IClassFixture<LoopbackHttpService>
{
    private const string ArchiveRtId = "6700000000000000000000b1";
    private const string Path = "/acme/v1/streamdata/archives/" + ArchiveRtId + "/insert";
    private const string WriteResult = """{"stored":1,"skipped":0,"skippedByReason":{},"skippedRows":[]}""";
    private readonly LoopbackHttpService _service;

    public StreamDataInsertRawClientTests(LoopbackHttpService service)
    {
        _service = service;
        _service.Reset();
    }

    private static RawPointDto Point(StreamDataOrigin? origin = null, string? reason = null) => new()
    {
        RtId = "6700000000000000000000c1",
        CkTypeId = "Basic.Energy/EnergyMeasurement",
        Timestamp = new DateTime(2026, 3, 1, 10, 15, 0, 123, DateTimeKind.Utc),
        Attributes = new Dictionary<string, object?> { ["Amount.Value"] = 1.5, ["Amount.Unit"] = null },
        Origin = origin ?? StreamDataOrigin.Automated,
        Reason = reason
    };

    [Fact]
    public async Task InsertAsync_WithoutOrigin_PostsAnAutomatedPointToTheRawRoute()
    {
        _service.RespondWith(Path, 200, WriteResult);

        await CreateClient().InsertAsync("acme", ArchiveRtId, [Point()]);

        Assert.Equal("POST " + Path, _service.SingleRequest());
        var point = JsonDocument.Parse(_service.Bodies.Single()).RootElement[0];
        Assert.Equal("6700000000000000000000c1", point.GetProperty("rtId").GetString());
        Assert.Equal("Basic.Energy/EnergyMeasurement", point.GetProperty("ckTypeId").GetString());
        Assert.Equal(new DateTime(2026, 3, 1, 10, 15, 0, 123, DateTimeKind.Utc),
            point.GetProperty("timestamp").GetDateTime().ToUniversalTime());
        Assert.Equal("automated", point.GetProperty("origin").GetString());
        Assert.Equal(JsonValueKind.Null, point.GetProperty("reason").ValueKind);
        Assert.Equal(1.5, point.GetProperty("attributes").GetProperty("Amount.Value").GetDouble());
        Assert.False(point.TryGetProperty("from", out _));
    }

    [Fact]
    public async Task InsertAsync_AnExplicitNullAttribute_IsSent()
    {
        // A manual point must name every ingested column; null is how a caller says "this column holds nothing".
        _service.RespondWith(Path, 200, WriteResult);

        await CreateClient().InsertAsync("acme", ArchiveRtId, [Point(StreamDataOrigin.Manual)]);

        var attributes = JsonDocument.Parse(_service.Bodies.Single()).RootElement[0].GetProperty("attributes");
        Assert.True(attributes.TryGetProperty("Amount.Unit", out var unit));
        Assert.Equal(JsonValueKind.Null, unit.ValueKind);
    }

    [Fact]
    public async Task InsertAsync_ManualWithReason_SendsBothAndNoWriter()
    {
        _service.RespondWith(Path, 200, WriteResult);

        await CreateClient().InsertAsync("acme", ArchiveRtId, [Point(StreamDataOrigin.Manual, "meter exchanged")]);

        var point = JsonDocument.Parse(_service.Bodies.Single()).RootElement[0];
        Assert.Equal("manual", point.GetProperty("origin").GetString());
        Assert.Equal("meter exchanged", point.GetProperty("reason").GetString());
        Assert.False(point.TryGetProperty("writtenBy", out _));
        Assert.False(point.TryGetProperty("manualWrittenBy", out _));
    }

    [Fact]
    public async Task InsertAsync_UnspecifiedKind_IsSentAsUtcNotLocal()
    {
        _service.RespondWith(Path, 200, WriteResult);
        var point = Point() with { Timestamp = new DateTime(2026, 3, 1, 10, 15, 0, DateTimeKind.Unspecified) };

        await CreateClient().InsertAsync("acme", ArchiveRtId, [point]);

        var sent = JsonDocument.Parse(_service.Bodies.Single()).RootElement[0];
        Assert.Equal(new DateTime(2026, 3, 1, 10, 15, 0, DateTimeKind.Utc), sent.GetProperty("timestamp").GetDateTime().ToUniversalTime());
    }

    [Fact]
    public async Task InsertAsync_EmptyBatch_SendsNothing()
    {
        var result = await CreateClient().InsertAsync("acme", ArchiveRtId, []);

        Assert.Empty(_service.Requests);
        Assert.Equal(0, result.Stored);
        Assert.Equal(0, result.Skipped);
        Assert.Empty(result.SkippedRows);
    }

    [Fact]
    public async Task InsertAsync_ReadsTheWriteResult_ASkippedRawRowHasNoTo()
    {
        _service.RespondWith(Path, 200, """
            {"stored":0,"skipped":1,"skippedByReason":{"ManualValueProtected":1},
             "skippedRows":[{"rtId":"6700000000000000000000c1","from":"2026-03-01T10:15:00.123Z","to":null,
                             "origin":"automated","reason":"ManualValueProtected"}]}
            """);

        var result = await CreateClient().InsertAsync("acme", ArchiveRtId, [Point()]);

        Assert.Equal(0, result.Stored);
        Assert.Equal(1, result.SkippedByReason["ManualValueProtected"]);
        var row = Assert.Single(result.SkippedRows);
        Assert.Equal(new DateTime(2026, 3, 1, 10, 15, 0, 123, DateTimeKind.Utc), row.From.ToUniversalTime());
        Assert.Null(row.To);
    }

    [Theory]
    [InlineData(400, "\"Point 0: attribute 'Voltage' is not a column of archive.\"")]
    [InlineData(403, """{"statusCode":403,"statusDescription":"Forbidden","message":"Access is denied: a write with origin 'manual' requires the role 'StreamDataAdmin' in the tenant."}""")]
    [InlineData(409, """{"message":"1 manual row(s) were not stored","result":{"stored":0,"skipped":1}}""")]
    public async Task InsertAsync_Refused_Throws(int status, string body)
    {
        _service.RespondWith(Path, status, body);

        await Assert.ThrowsAnyAsync<Exception>(() => CreateClient().InsertAsync("acme", ArchiveRtId, [Point()]));
    }

    [Theory]
    [InlineData(204, "")]
    [InlineData(200, "not a write result")]
    public async Task InsertAsync_WithoutAReadableResult_DoesNotReportRowsAsStored(int status, string body)
    {
        _service.RespondWith(Path, status, body);

        await Assert.ThrowsAnyAsync<Exception>(() => CreateClient().InsertAsync("acme", ArchiveRtId, [Point(), Point()]));
    }

    private StreamDataServicesClient CreateClient()
    {
        var options = new StreamDataServiceClientOptions { EndpointUri = _service.BaseUrl, TenantId = "acme" };
        return new StreamDataServicesClient(options, A.Fake<IStreamDataServiceClientAccessToken>());
    }
}
