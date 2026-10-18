using System.Net;
using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.CkModelCatalog;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.System;
using Sdk.ServiceClient.Tests.BotServices;

namespace Sdk.ServiceClient.Tests.AssetRepositoryServices.CkModelCatalog;

/// <summary>
///     AB#6870: the import calls surface the tenant upgrade guard refusal (HTTP 409 <c>CK_UPGRADE_BLOCKED</c>) as a
///     <see cref="CkUpgradeBlockedException" /> with the structured blockers, and send the force flag when asked to.
/// </summary>
public class ImportFromCatalogUpgradeGuardTests : IClassFixture<LoopbackHttpService>
{
    private const string BlockedBody = """
        {
          "type": "https://schemas.meshmakers.cloud/errors/ck-upgrade-blocked",
          "title": "The update would break installed CK models",
          "status": 409,
          "detail": "Importing Basic-2.5.0 would break 1 installed model(s). Nothing was imported.",
          "code": "CK_UPGRADE_BLOCKED",
          "updateSet": ["Basic-2.5.0"],
          "blockers": [{
            "dependent": "EnergyCommunity.Registration-2.8.0", "update": "Basic-2.5.0", "kind": "ExactPin",
            "subject": "Basic-2.4.0", "code": "CK_UPGRADE_EXACT_PIN", "message": "m", "dependentIsInUpdateSet": false,
            "suggestedDependentUpdate": "EnergyCommunity.Registration-2.10.0",
            "resolveHint": "update EnergyCommunity.Registration to 2.10.0 first"
          }]
        }
        """;

    private readonly LoopbackHttpService _service;
    private readonly AssetServicesClient _client;

    public ImportFromCatalogUpgradeGuardTests(LoopbackHttpService service)
    {
        _service = service;
        _service.Reset();
        _client = new AssetServicesClient(new AssetServiceClientOptions { EndpointUri = _service.BaseUrl },
            A.Fake<IAssetServiceClientAccessToken>());
    }

    [Fact]
    public async Task ImportFromCatalogBatchAsync_409CkUpgradeBlocked_ThrowsTheStructuredException()
    {
        _service.RespondWith("/acme/v1/models/ImportFromCatalogBatch", 409, BlockedBody);

        var ex = await Assert.ThrowsAsync<CkUpgradeBlockedException>(() => _client.ImportFromCatalogBatchAsync("acme",
            new ImportFromCatalogBatchRequestDto { CatalogName = "c", ModelIds = ["Basic-2.5.0"] }));

        Assert.Equal(HttpStatusCode.Conflict, ex.HttpStatusCode);
        Assert.Equal(["Basic-2.5.0"], ex.UpdateSet);
        var blocker = Assert.Single(ex.Blockers);
        Assert.Equal("EnergyCommunity.Registration-2.8.0", blocker.Dependent);
        Assert.Equal("Basic-2.4.0", blocker.Subject);
        Assert.Equal("update EnergyCommunity.Registration to 2.10.0 first", blocker.ResolveHint);
        Assert.Contains("CK_UPGRADE_BLOCKED", ex.Message);
    }

    [Fact]
    public async Task ImportFromCatalogAsync_409CkUpgradeBlocked_ThrowsTheStructuredException()
    {
        _service.RespondWith("/acme/v1/models/ImportFromCatalog", 409, BlockedBody);

        var ex = await Assert.ThrowsAsync<CkUpgradeBlockedException>(() => _client.ImportFromCatalogAsync("acme",
            new ImportFromCatalogRequestDto { CatalogName = "c", ModelId = "Basic-2.5.0" }));

        Assert.Single(ex.Blockers);
    }

    [Fact]
    public async Task ImportFromCatalogBatchAsync_AnotherConflict_StaysAGenericResultException()
    {
        _service.RespondWith("/acme/v1/models/ImportFromCatalogBatch", 409, """{"code":"SOMETHING_ELSE"}""");

        var ex = await Assert.ThrowsAsync<ServiceClientResultException>(() => _client.ImportFromCatalogBatchAsync(
            "acme", new ImportFromCatalogBatchRequestDto { CatalogName = "c", ModelIds = ["Basic-2.5.0"] }));

        Assert.IsNotType<CkUpgradeBlockedException>(ex);
        Assert.Equal(HttpStatusCode.Conflict, ex.HttpStatusCode);
    }

    [Fact]
    public async Task ImportFromCatalogAsync_ReadsTheJobIdTheServiceReturns()
    {
        var result = await _client.ImportFromCatalogAsync("acme",
            new ImportFromCatalogRequestDto { CatalogName = "c", ModelId = "Basic-2.5.0", ForceBreakingUpdate = true });

        Assert.Equal("POST /acme/v1/models/ImportFromCatalog", _service.SingleRequest());
        Assert.Equal("job-1", result.JobId);
    }

    [Fact]
    public void ForceFlag_IsSentAsForceBreakingUpdate()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(
            new ImportFromCatalogBatchRequestDto { CatalogName = "c", ModelIds = ["a-1.0.0"], ForceBreakingUpdate = true },
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));

        Assert.Contains("\"forceBreakingUpdate\":true", json);
    }

    [Fact]
    public void RequestDtos_DefaultToNoForce_AndOldResolveResponsesDeserialize()
    {
        Assert.False(new ImportFromCatalogRequestDto().ForceBreakingUpdate);
        Assert.False(new ImportFromCatalogBatchRequestDto().ForceBreakingUpdate);

        var old = System.Text.Json.JsonSerializer.Deserialize<BatchDependencyResolutionResponseDto>(
            """{"modelsToImport":["Basic-2.5.0"],"dependencyTrees":[]}""",
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))!;
        Assert.False(old.IsBlocked);
        Assert.Empty(old.Blockers);
    }
    // AB#6966: the file import (ImportCk) is guarded like the catalog imports.

    private static string CkFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ab6966-{Guid.NewGuid():N}.yaml");
        File.WriteAllText(path, "modelId: System-2.8.0");
        return path;
    }

    [Fact]
    public async Task ImportCkModelAsync_409CkUpgradeBlocked_ThrowsTheStructuredException()
    {
        _service.RespondWith("/acme/v1/models/ImportCk", 409, BlockedBody);
        var file = CkFile();
        try
        {
            var ex = await Assert.ThrowsAsync<CkUpgradeBlockedException>(() => _client.ImportCkModelAsync("acme", file));

            Assert.Equal(HttpStatusCode.Conflict, ex.HttpStatusCode);
            Assert.Equal(["Basic-2.5.0"], ex.UpdateSet);
            var blocker = Assert.Single(ex.Blockers);
            Assert.Equal("EnergyCommunity.Registration-2.8.0", blocker.Dependent);
            Assert.Contains("CK_UPGRADE_BLOCKED", ex.Message);
            Assert.DoesNotContain("forceBreakingUpdate", Assert.Single(_service.Bodies));
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public async Task ImportCkModelAsync_Forced_SendsTheFormMemberNextToTheFile_AndReadsTheJobId()
    {
        var file = CkFile();
        try
        {
            var jobId = await _client.ImportCkModelAsync("acme", file, forceBreakingUpdate: true);

            Assert.Equal("job-1", jobId);
            Assert.Equal("POST /acme/v1/models/ImportCk", _service.SingleRequest());
            var body = Assert.Single(_service.Bodies);
            Assert.Matches("name=\"?forceBreakingUpdate\"?", body);
            Assert.Contains("true", body);
            Assert.Contains("modelId: System-2.8.0", body);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public async Task ImportCkModelAsync_AnotherConflict_StaysAGenericResultException()
    {
        _service.RespondWith("/acme/v1/models/ImportCk", 409, """{"code":"SOMETHING_ELSE"}""");
        var file = CkFile();
        try
        {
            var ex = await Assert.ThrowsAsync<ServiceClientResultException>(() => _client.ImportCkModelAsync("acme", file));

            Assert.IsNotType<CkUpgradeBlockedException>(ex);
        }
        finally
        {
            File.Delete(file);
        }
    }
}
