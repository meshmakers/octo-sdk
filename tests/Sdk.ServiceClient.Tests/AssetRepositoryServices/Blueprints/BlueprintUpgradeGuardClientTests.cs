using System.Net;
using System.Text.Json;
using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Blueprints;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.CkModelCatalog;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.System;
using Sdk.ServiceClient.Tests.BotServices;

namespace Sdk.ServiceClient.Tests.AssetRepositoryServices.Blueprints;

/// <summary>
///     AB#6871: the blueprint apply calls surface the tenant upgrade guard refusal (HTTP 409 <c>CK_UPGRADE_BLOCKED</c>)
///     as a <see cref="CkUpgradeBlockedException" />, preview, dry run and results carry the blockers, and the force
///     flag travels as <c>forceBreakingUpdate</c>. Services without the guard keep working.
/// </summary>
public class BlueprintUpgradeGuardClientTests : IClassFixture<LoopbackHttpService>
{
    private const string BlockedBody = """
        {
          "type": "https://schemas.meshmakers.cloud/errors/ck-upgrade-blocked",
          "title": "The update would break installed CK models",
          "status": 409,
          "detail": "Blueprint Shop-2.0.0 would install Basic-2.5.0, which would break 1 installed model(s). Nothing was changed.",
          "code": "CK_UPGRADE_BLOCKED",
          "updateSet": ["Basic-2.5.0"],
          "blockers": [{
            "dependent": "EnergyCommunity.Registration-2.8.0", "update": "Basic-2.5.0", "kind": "ExactPin",
            "subject": "Basic-2.4.0", "code": "CK_UPGRADE_EXACT_PIN", "message": "m", "dependentIsInUpdateSet": false
          }]
        }
        """;

    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private readonly LoopbackHttpService _service;
    private readonly AssetServicesClient _client;

    public BlueprintUpgradeGuardClientTests(LoopbackHttpService service)
    {
        _service = service;
        _service.Reset();
        _client = new AssetServicesClient(new AssetServiceClientOptions { EndpointUri = _service.BaseUrl },
            A.Fake<IAssetServiceClientAccessToken>());
    }

    [Fact]
    public async Task ApplyBlueprintAsync_409CkUpgradeBlocked_ThrowsTheStructuredException()
    {
        _service.RespondWith("/acme/v1/blueprints/apply", 409, BlockedBody);

        var ex = await Assert.ThrowsAsync<CkUpgradeBlockedException>(() =>
            _client.ApplyBlueprintAsync("acme", new BlueprintApplyRequestDto { BlueprintId = "Shop-2.0.0" }));

        Assert.Equal(HttpStatusCode.Conflict, ex.HttpStatusCode);
        Assert.Equal(["Basic-2.5.0"], ex.UpdateSet);
        var blocker = Assert.Single(ex.Blockers);
        Assert.Equal("EnergyCommunity.Registration-2.8.0", blocker.Dependent);
        Assert.Equal("Basic-2.4.0", blocker.Subject);
    }

    [Fact]
    public async Task ApplyBlueprintAsync_LegacyOverload_AlsoThrowsTheStructuredException()
    {
        _service.RespondWith("/acme/v1/blueprints/apply", 409, BlockedBody);

        await Assert.ThrowsAsync<CkUpgradeBlockedException>(() => _client.ApplyBlueprintAsync("acme", "Shop-2.0.0"));
    }

    [Fact]
    public async Task ApplyBlueprintUpdateAsync_409CkUpgradeBlocked_ThrowsTheStructuredException()
    {
        _service.RespondWith("/acme/v1/blueprints/updates/apply", 409, BlockedBody);

        var ex = await Assert.ThrowsAsync<CkUpgradeBlockedException>(() => _client.ApplyBlueprintUpdateAsync("acme",
            new BlueprintUpdateRequestDto { TargetVersion = "Shop-2.0.0" }));

        Assert.Single(ex.Blockers);
    }

    [Fact]
    public async Task ApplyBlueprintUpdateAsync_AnotherConflict_StaysAGenericResultException()
    {
        _service.RespondWith("/acme/v1/blueprints/updates/apply", 409, """{"code":"BLUEPRINT_LOCKED"}""");

        var ex = await Assert.ThrowsAsync<ServiceClientResultException>(() => _client.ApplyBlueprintUpdateAsync("acme",
            new BlueprintUpdateRequestDto { TargetVersion = "Shop-2.0.0" }));

        Assert.IsNotType<CkUpgradeBlockedException>(ex);
    }

    [Fact]
    public async Task PreviewBlueprintUpdateAsync_ReadsTheBlockers()
    {
        _service.RespondWith("/acme/v1/blueprints/updates/preview", 200, """
            {
              "targetVersion": "Shop-2.0.0", "entitiesToAdd": 0,
              "isBlocked": true, "ckUpdateSet": ["Basic-2.5.0"],
              "blockers": [{ "dependent": "EnergyCommunity.Registration-2.8.0", "update": "Basic-2.5.0",
                "kind": "ExactPin", "subject": "Basic-2.4.0", "code": "CK_UPGRADE_EXACT_PIN", "message": "m" }]
            }
            """);

        var preview = await _client.PreviewBlueprintUpdateAsync("acme",
            new BlueprintUpdateRequestDto { TargetVersion = "Shop-2.0.0", DryRun = true });

        Assert.True(preview.IsBlocked);
        Assert.Equal(["Basic-2.5.0"], preview.CkUpdateSet);
        Assert.Equal("Basic-2.4.0", Assert.Single(preview.Blockers).Subject);
    }

    [Fact]
    public void ForceFlag_IsSentAsForceBreakingUpdate_AndDefaultsToOff()
    {
        Assert.Contains("\"forceBreakingUpdate\":true", JsonSerializer.Serialize(
            new BlueprintUpdateRequestDto { TargetVersion = "Shop-2.0.0", ForceBreakingUpdate = true }, Web));
        Assert.Contains("\"forceBreakingUpdate\":true", JsonSerializer.Serialize(
            new BlueprintApplyRequestDto { BlueprintId = "Shop-2.0.0", ForceBreakingUpdate = true }, Web));
        Assert.False(new BlueprintUpdateRequestDto().ForceBreakingUpdate);
        Assert.False(new BlueprintApplyRequestDto().ForceBreakingUpdate);
    }

    [Fact]
    public void ResultsOfServicesWithoutTheGuard_DeserializeWithEmptyBlockers()
    {
        var update = JsonSerializer.Deserialize<BlueprintUpdateResultDto>("""{"success":true}""", Web)!;
        var preview = JsonSerializer.Deserialize<BlueprintUpdatePreviewDto>("""{"targetVersion":"Shop-2.0.0"}""", Web)!;
        var apply = JsonSerializer.Deserialize<BlueprintApplyResultDto>("""{"success":true}""", Web)!;

        Assert.False(update.IsBlocked);
        Assert.Empty(update.Blockers);
        Assert.False(preview.IsBlocked);
        Assert.Empty(preview.CkUpdateSet);
        Assert.Empty(apply.Blockers);
    }
}
