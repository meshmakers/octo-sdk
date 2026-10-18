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
    public async Task PlanRollupRepairAsync_IsADryRunRequest()
    {
        _service.RespondWith(RepairPath, 200, DryRunReport);
        var client = CreateClient();

        var report = await client.PlanRollupRepairAsync("acme");

        Assert.Equal("POST /acme/v1/streamdata/rollups/repair?dryRun=true", _service.SingleRequest());
        Assert.True(report.DryRun);
        Assert.Null(report.After);
        var action = Assert.Single(report.Actions);
        Assert.Equal("DeleteOrphans", action.Kind);
        Assert.Equal("Planned", action.Status);
    }

    [Fact]
    public async Task EnqueueRollupRepairAsync_Accepts202WithTheRequest()
    {
        _service.RespondWith(RepairPath, 202, PendingRequest);
        var client = CreateClient();

        var request = await client.EnqueueRollupRepairAsync("acme", ["r1"]);

        Assert.Equal("POST /acme/v1/streamdata/rollups/repair?rollupRtId=r1&dryRun=false", _service.SingleRequest());
        Assert.Equal("req-1", request.RequestId);
        Assert.Equal("Pending", request.State);
        Assert.False(request.IsFinished);
        Assert.Null(request.FinishedAtUtc);
        Assert.Null(request.Report);
        Assert.Single(request.PlannedActions);
    }

    [Fact]
    public async Task GetRollupRepairRequestAsync_ReadsTheFinalReport()
    {
        _service.RespondWith(RepairPath + "/req-1", 200, DoneRequest);
        var client = CreateClient();

        var request = await client.GetRollupRepairRequestAsync("acme", "req-1");

        Assert.Equal("GET /acme/v1/streamdata/rollups/repair/req-1", _service.SingleRequest());
        Assert.True(request.IsFinished);
        Assert.Equal("Done", request.State);
        Assert.NotNull(request.FinishedAtUtc);
        Assert.True(request.Report!.IsOk);
        Assert.True(request.Report.After!.IsOk);
    }

    [Fact]
    public async Task GetRollupRepairRequestAsync_EscapesTheRequestId()
    {
        _service.RespondWith(RepairPath + "/..%2Fx", 200, DoneRequest);
        var client = CreateClient();

        await client.GetRollupRepairRequestAsync("acme", "../x");

        Assert.StartsWith("GET /acme/v1/streamdata/rollups/repair/..%2Fx", _service.SingleRequest());
    }

    [Fact]
    public async Task EnqueueRollupRepairAsync_Refused_ThrowsWithTheStatus()
    {
        _service.RespondWith(RepairPath, 400, "Stream data is not enabled");
        var client = CreateClient();

        var exception = await Assert.ThrowsAsync<ServiceClientResultException>(() =>
            client.EnqueueRollupRepairAsync("acme"));

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
             "status": "Planned", "rangeFromUtc": null, "rangeToUtc": null, "generation": 3, "error": null}
          ],
          "after": null,
          "failedStep": null
        }
        """;

    private const string PendingRequest = """
        {
          "requestId": "req-1",
          "state": "Pending",
          "requestedAtUtc": "2026-10-18T10:00:00Z",
          "finishedAtUtc": null,
          "plannedActions": [
            {"rollupRtId": "r1", "kind": "RecomputePointerRange", "description": "recompute the range",
             "status": "Planned", "rangeFromUtc": "2026-10-01T00:00:00Z", "rangeToUtc": "2026-10-02T00:00:00Z",
             "generation": null, "error": null}
          ],
          "report": null
        }
        """;

    private const string DoneRequest = """
        {
          "requestId": "req-1",
          "state": "Done",
          "requestedAtUtc": "2026-10-18T10:00:00Z",
          "finishedAtUtc": "2026-10-18T10:20:00Z",
          "plannedActions": [],
          "report": {
            "dryRun": false,
            "isOk": true,
            "before": {"isOk": false, "checkedAtUtc": "2026-10-18T10:00:00Z", "entries": []},
            "actions": [
              {"rollupRtId": "r1", "kind": "RecomputePointerRange", "description": "recompute the range",
               "status": "Done", "rangeFromUtc": null, "rangeToUtc": null, "generation": null, "error": null}
            ],
            "after": {"isOk": true, "checkedAtUtc": "2026-10-18T10:20:00Z", "entries": []},
            "failedStep": null
          }
        }
        """;

    private StreamDataServicesClient CreateClient()
    {
        var options = new StreamDataServiceClientOptions { EndpointUri = _service.BaseUrl, TenantId = "acme" };
        return new StreamDataServicesClient(options, A.Fake<IStreamDataServiceClientAccessToken>());
    }
}
