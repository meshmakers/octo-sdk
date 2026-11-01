using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.StreamData;
using Sdk.ServiceClient.Tests.BotServices;

namespace Sdk.ServiceClient.Tests.AssetRepositoryServices.StreamData;

/// <summary>
///     AB#8002 — retrying a failed computed-column backfill, and the backfill job fields of the recompute-jobs list,
///     pinned on the wire against a loopback service.
/// </summary>
public class ComputedColumnRetryClientTests : IClassFixture<LoopbackHttpService>
{
    private const string Archive = "6a0e000000000000000a0007";
    private readonly LoopbackHttpService _service;

    public ComputedColumnRetryClientTests(LoopbackHttpService service)
    {
        _service = service;
        _service.Reset();
    }

    [Fact]
    public async Task RetryComputedColumnBackfillAsync_PostsTheRetryRoute_AndReadsTheQueuedJob()
    {
        _service.RespondWith($"/acme/v1/streamdata/archives/{Archive}/computed-columns/ExportPower/retry", 200,
            """{"rtId":"6a0e000000000000000a0099","state":"Pending","rowsProcessed":null,"windowsProcessed":null,"startedAt":null,"finishedAt":null,"durationMs":null,"errorReason":null,"lastProgressAt":"2026-11-01T10:00:00Z","computedColumnName":"ExportPower"}""");

        var job = await CreateClient().RetryComputedColumnBackfillAsync("acme", Archive, "ExportPower");

        Assert.Equal($"POST /acme/v1/streamdata/archives/{Archive}/computed-columns/ExportPower/retry", _service.SingleRequest());
        Assert.NotNull(job);
        Assert.Equal("Pending", job!.State);
        Assert.Equal("ExportPower", job.ComputedColumnName);
    }

    [Fact]
    public async Task RetryComputedColumnBackfillAsync_ActiveColumn_SurfacesTheServerRefusal()
    {
        _service.RespondWith($"/acme/v1/streamdata/archives/{Archive}/computed-columns/ExportPower/retry", 400,
            "\"computed column 'ExportPower' is Active; there is no failed backfill to retry.\"");

        await Assert.ThrowsAnyAsync<Exception>(() =>
            CreateClient().RetryComputedColumnBackfillAsync("acme", Archive, "ExportPower"));
    }

    [Fact]
    public async Task ListRecomputeJobsForArchiveAsync_ReadsTheComputedColumnOfABackfillJob()
    {
        _service.RespondWith($"/acme/v1/streamdata/archives/{Archive}/recompute-jobs", 200,
            """[{"rtId":"a1","state":"Running","rowsProcessed":12000,"windowsProcessed":null,"startedAt":"2026-11-01T10:00:00Z","finishedAt":null,"durationMs":null,"errorReason":null,"computedColumnName":"ImportPower"},{"rtId":"a2","state":"Completed","rowsProcessed":10,"windowsProcessed":2,"startedAt":null,"finishedAt":null,"durationMs":5,"errorReason":null}]""");

        var jobs = await CreateClient().ListRecomputeJobsForArchiveAsync("acme", Archive);

        Assert.Equal("ImportPower", jobs[0].ComputedColumnName);
        Assert.Equal(12000, jobs[0].RowsProcessed);
        Assert.Null(jobs[1].ComputedColumnName);
    }

    private StreamDataServicesClient CreateClient()
    {
        var options = new StreamDataServiceClientOptions { EndpointUri = _service.BaseUrl, TenantId = "acme" };
        return new StreamDataServicesClient(options, A.Fake<IStreamDataServiceClientAccessToken>());
    }
}
