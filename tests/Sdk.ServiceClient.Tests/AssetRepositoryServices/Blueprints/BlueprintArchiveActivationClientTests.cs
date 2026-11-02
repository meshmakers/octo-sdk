using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Blueprints;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.System;
using Sdk.ServiceClient.Tests.BotServices;

namespace Sdk.ServiceClient.Tests.AssetRepositoryServices.Blueprints;

/// <summary>
///     AB#7991: the blueprint install and update results carry the archive activations the service reports (activated,
///     skipped with a reason, failed, backfill job), so octo-cli and the MCP tools no longer drop them. Services without
///     the field keep working.
/// </summary>
public class BlueprintArchiveActivationClientTests : IClassFixture<LoopbackHttpService>
{
    private const string Activations = """
        "archiveActivations": [
          { "archiveRtId": "6a4aa4cc22664f2b26c67e6b", "wellKnownName": "GridConnectionRollup1h",
            "ckTypeId": "System.StreamData/RollupArchive", "blueprintId": "EnergyIQ.Base-1.1.0", "isRollup": true,
            "outcome": "Activated", "reason": null, "backfill": "Queued", "backfillJobRtId": "6ae5b33b2a27b45f5568bee8",
            "backfillReason": null },
          { "archiveRtId": "6a0e000000000000000a0007", "wellKnownName": null,
            "ckTypeId": "System.StreamData/RawArchive", "blueprintId": "EnergyIQ.Base-1.1.0", "isRollup": false,
            "outcome": "SkippedStreamDataDisabled", "reason": "Stream data is not enabled", "backfill": "NotApplicable",
            "backfillJobRtId": null, "backfillReason": null }
        ]
        """;

    private readonly LoopbackHttpService _service;
    private readonly AssetServicesClient _client;

    public BlueprintArchiveActivationClientTests(LoopbackHttpService service)
    {
        _service = service;
        _service.Reset();
        _client = new AssetServicesClient(new AssetServiceClientOptions { EndpointUri = _service.BaseUrl },
            A.Fake<IAssetServiceClientAccessToken>());
    }

    [Fact]
    public async Task ApplyBlueprintAsync_ReturnsTheArchiveActivations()
    {
        _service.RespondWith("/acme/v1/blueprints/apply", 200,
            "{ \"success\": true, \"blueprintId\": \"EnergyIQ.Base-1.1.0\", \"warnings\": [\"w\"], " + Activations + " }");

        var result = await _client.ApplyBlueprintAsync("acme", "EnergyIQ.Base-1.1.0");

        Assert.Equal(2, result.ArchiveActivations.Count);
        var rollup = result.ArchiveActivations[0];
        Assert.Equal("GridConnectionRollup1h", rollup.WellKnownName);
        Assert.True(rollup.IsRollup);
        Assert.Equal("Activated", rollup.Outcome);
        Assert.Equal("Queued", rollup.Backfill);
        Assert.Equal("6ae5b33b2a27b45f5568bee8", rollup.BackfillJobRtId);
        var skipped = result.ArchiveActivations[1];
        Assert.Equal("SkippedStreamDataDisabled", skipped.Outcome);
        Assert.Equal("Stream data is not enabled", skipped.Reason);
    }

    [Fact]
    public async Task ApplyBlueprintUpdateAsync_ReturnsTheArchiveActivations()
    {
        _service.RespondWith("/acme/v1/blueprints/updates/apply", 200,
            "{ \"success\": true, \"entitiesAdded\": 3, " + Activations + " }");

        var result = await _client.ApplyBlueprintUpdateAsync("acme",
            new BlueprintUpdateRequestDto { TargetVersion = "EnergyIQ.Base-1.1.0" });

        Assert.Equal(2, result.ArchiveActivations.Count);
        Assert.Equal("6ae5b33b2a27b45f5568bee8", result.ArchiveActivations[0].BackfillJobRtId);
    }

    [Fact]
    public async Task ApplyBlueprintAsync_ServiceWithoutTheField_ReadsAnEmptyList()
    {
        _service.RespondWith("/acme/v1/blueprints/apply", 200, "{ \"success\": true }");

        var result = await _client.ApplyBlueprintAsync("acme", "Shop-1.0.0");

        Assert.Empty(result.ArchiveActivations);
    }
}
