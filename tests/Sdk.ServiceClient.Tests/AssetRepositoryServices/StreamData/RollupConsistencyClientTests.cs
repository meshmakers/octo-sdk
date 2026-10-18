using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.StreamData;
using Sdk.ServiceClient.Tests.BotServices;

namespace Sdk.ServiceClient.Tests.AssetRepositoryServices.StreamData;

/// <summary>
///     AB#6968 — the operator surface for rollup validate and repair, pinned on the wire against a loopback service
///     (verb, route, query and the deserialised report).
/// </summary>
public class RollupConsistencyClientTests : IClassFixture<LoopbackHttpService>
{
    private const string ValidatePath = "/acme/v1/streamdata/rollups/validate";
    private const string RepairPath = "/acme/v1/streamdata/rollups/repair";

    private const string FindingReport = """
        {
          "isOk": false,
          "checkedAtUtc": "2026-10-18T10:00:00Z",
          "entries": [
            {
              "rollupRtId": "6700000000000000000000a1",
              "isOk": false,
              "findings": [
                {
                  "check": "OrphansAbovePointer",
                  "message": "rows above the pointer",
                  "rangeFromUtc": "2026-10-01T00:00:00Z",
                  "rangeToUtc": "2026-10-02T00:00:00Z",
                  "generation": 3,
                  "rows": 120,
                  "gap": null
                }
              ]
            }
          ]
        }
        """;

    private readonly LoopbackHttpService _service;

    public RollupConsistencyClientTests(LoopbackHttpService service)
    {
        _service = service;
        _service.Reset();
    }

    [Fact]
    public async Task ValidateRollupsAsync_WithoutIds_GetsTheTenantRouteAndReadsTheReport()
    {
        _service.RespondWith(ValidatePath, 200, FindingReport);
        var client = CreateClient();

        var report = await client.ValidateRollupsAsync("acme");

        Assert.Equal("GET /acme/v1/streamdata/rollups/validate", _service.SingleRequest());
        Assert.False(report.IsOk);
        var entry = Assert.Single(report.Entries);
        Assert.Equal("6700000000000000000000a1", entry.RollupRtId);
        var finding = Assert.Single(entry.Findings);
        Assert.Equal("OrphansAbovePointer", finding.Check);
        Assert.Equal(3, finding.Generation);
        Assert.Equal(120, finding.Rows);
    }

    [Fact]
    public async Task ValidateRollupsAsync_WithIds_SendsOneQueryParameterPerRollup()
    {
        _service.RespondWith(ValidatePath, 200, """{"isOk":true,"checkedAtUtc":"2026-10-18T10:00:00Z","entries":[]}""");
        var client = CreateClient();

        var report = await client.ValidateRollupsAsync("acme", ["r1", "r2"]);

        Assert.Equal("GET /acme/v1/streamdata/rollups/validate?rollupRtId=r1&rollupRtId=r2", _service.SingleRequest());
        Assert.True(report.IsOk);
    }

    [Fact]
    public async Task RepairRollupsAsync_DefaultsToDryRun()
    {
        _service.RespondWith(RepairPath, 200, DryRunReport);
        var client = CreateClient();

        var report = await client.RepairRollupsAsync("acme");

        Assert.Equal("POST /acme/v1/streamdata/rollups/repair?dryRun=true", _service.SingleRequest());
        Assert.True(report.DryRun);
        Assert.Null(report.After);
        var action = Assert.Single(report.Actions);
        Assert.Equal("DeleteOrphans", action.Kind);
        Assert.Equal("Planned", action.Status);
        Assert.Null(action.JobId);
    }

    [Fact]
    public async Task RepairRollupsAsync_Apply_Accepts202WithTheEnqueuedJobIds()
    {
        _service.RespondWith(RepairPath, 202, AppliedReport);
        var client = CreateClient();

        var report = await client.RepairRollupsAsync("acme", ["r1"], dryRun: false);

        Assert.Equal("POST /acme/v1/streamdata/rollups/repair?rollupRtId=r1&dryRun=false", _service.SingleRequest());
        Assert.False(report.DryRun);
        Assert.Equal(["job-1"], report.JobIds);
        Assert.Contains(report.Actions, a => a.Status == "Enqueued" && a.JobId == "job-1");
        Assert.Null(report.After);
    }

    [Fact]
    public async Task RepairRollupsAsync_Refused_ThrowsWithTheStatus()
    {
        _service.RespondWith(RepairPath, 400, "Stream data is not enabled");
        var client = CreateClient();

        var exception = await Assert.ThrowsAsync<ServiceClientResultException>(() =>
            client.RepairRollupsAsync("acme", dryRun: false));

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, exception.HttpStatusCode);
    }

    [Fact]
    public async Task ValidateRollupsAsync_BlankTenant_Throws()
    {
        var client = CreateClient();

        await Assert.ThrowsAnyAsync<ArgumentException>(() => client.ValidateRollupsAsync(""));
    }

    private const string DryRunReport = """
        {
          "dryRun": true,
          "isOk": true,
          "before": {"isOk": false, "checkedAtUtc": "2026-10-18T10:00:00Z", "entries": []},
          "actions": [
            {"rollupRtId": "r1", "kind": "DeleteOrphans", "description": "delete rows above the pointer",
             "status": "Planned", "rangeFromUtc": null, "rangeToUtc": null, "generation": 3, "error": null, "jobId": null}
          ],
          "after": null,
          "failedStep": null,
          "jobIds": []
        }
        """;

    private const string AppliedReport = """
        {
          "dryRun": false,
          "isOk": true,
          "before": {"isOk": false, "checkedAtUtc": "2026-10-18T10:00:00Z", "entries": []},
          "actions": [
            {"rollupRtId": "r1", "kind": "DeleteOrphans", "description": "delete rows above the pointer",
             "status": "Done", "rangeFromUtc": null, "rangeToUtc": null, "generation": 3, "error": null, "jobId": null},
            {"rollupRtId": "r1", "kind": "RecomputePointerRange", "description": "recompute the range",
             "status": "Enqueued", "rangeFromUtc": "2026-10-01T00:00:00Z", "rangeToUtc": "2026-10-02T00:00:00Z",
             "generation": null, "error": null, "jobId": "job-1"}
          ],
          "after": null,
          "failedStep": null,
          "jobIds": ["job-1"]
        }
        """;

    private StreamDataServicesClient CreateClient()
    {
        var options = new StreamDataServiceClientOptions { EndpointUri = _service.BaseUrl, TenantId = "acme" };
        return new StreamDataServicesClient(options, A.Fake<IStreamDataServiceClientAccessToken>());
    }
}
