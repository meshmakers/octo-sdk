using System.Net;
using System.Text;
using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Blueprints;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.System;
using Sdk.ServiceClient.Tests.TestSupport;

namespace Sdk.ServiceClient.Tests.AssetRepositoryServices.Blueprints;

/// <summary>
///     AB#8966: the SDK reads the erased and kept entities of an uninstall, calls the dry run on its own route (an older
///     service answers it with 404, never with an uninstall) and keeps working against services without the new fields.
/// </summary>
public sealed class BlueprintUninstallClientTests : IDisposable
{
    private readonly WireService _service = new();

    public void Dispose() => _service.Dispose();

    private const string Result = """
        {"success":true,"dryRun":true,"uninstalledBlueprintId":"System.Ai.Builder-1.0.0","entitiesDeleted":2,
         "deletedEntities":[
           {"key":"page-a","ckTypeId":"System.Ai/KnowledgePage","entityId":"aa0000000000000000000001","wellKnownName":"page-a","tenantOwned":true},
           {"key":"space","ckTypeId":"System.Ai/KnowledgeSpace","entityId":"aa0000000000000000000002","tenantOwned":false}],
         "tenantOwnedKept":[
           {"key":"page-b","ckTypeId":"System.Ai/KnowledgePage","entityId":"aa0000000000000000000003","reason":"Edited","referencedBy":[]},
           {"key":"page-b@1","ckTypeId":"System.Ai/KnowledgePageVersion","entityId":"aa0000000000000000000004","reason":"Referenced",
            "referencedBy":["aa0000000000000000000003"]}],
         "cascadedDependencies":[],"blockingDependents":[],"warnings":["w"]}
        """;

    [Fact]
    public async Task PreviewUninstall_PostsToThePreviewRoute_AndReadsDeletedAndKept()
    {
        _service.Respond(200, Result);

        var preview = await NewClient().PreviewUninstallBlueprintAsync("acme", "System.Ai.Builder", cascade: true);

        Assert.Equal("POST /acme/v1/blueprints/System.Ai.Builder/uninstall/preview?cascade=true", _service.SingleRequest());
        Assert.True(preview.DryRun);
        Assert.Equal(2, preview.EntitiesDeleted);
        Assert.Equal([("page-a", true), ("space", false)], preview.DeletedEntities.Select(e => (e.Key, e.TenantOwned)));
        Assert.Equal([("page-b", "Edited"), ("page-b@1", "Referenced")], preview.TenantOwnedKept.Select(e => (e.Key, e.Reason)));
        Assert.Equal(["aa0000000000000000000003"], preview.TenantOwnedKept[1].ReferencedBy);
    }

    [Fact]
    public async Task PreviewUninstall_AgainstAnOlderService_FailsWith404()
    {
        _service.Respond(404, string.Empty);

        var error = await Assert.ThrowsAsync<ServiceClientResultException>(
            () => NewClient().PreviewUninstallBlueprintAsync("acme", "System.Ai.Builder"));
        Assert.Equal(HttpStatusCode.NotFound, error.HttpStatusCode);
        Assert.Equal("POST /acme/v1/blueprints/System.Ai.Builder/uninstall/preview", _service.SingleRequest());
    }

    [Fact]
    public async Task Uninstall_StillDeletes_AndReadsTheNewLists()
    {
        _service.Respond(200, Result.Replace("\"dryRun\":true", "\"dryRun\":false"));

        var result = await NewClient().UninstallBlueprintAsync("acme", "System.Ai.Builder");

        Assert.Equal("DELETE /acme/v1/blueprints/System.Ai.Builder", _service.SingleRequest());
        Assert.False(result.DryRun);
        Assert.Equal(2, result.DeletedEntities.Count);
        Assert.Equal(2, result.TenantOwnedKept.Count);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task BlockedByDependents_ReturnsTheResultInsteadOfThrowing(bool dryRun)
    {
        _service.Respond(409, """{"success":false,"dryRun":true,"blockingDependents":["Addon-1.0.0"],"warnings":[]}""");

        var client = NewClient();
        var result = dryRun
            ? await client.PreviewUninstallBlueprintAsync("acme", "System.Ai.Builder")
            : await client.UninstallBlueprintAsync("acme", "System.Ai.Builder");

        Assert.False(result.Success);
        Assert.Equal(["Addon-1.0.0"], result.BlockingDependents);
    }

    [Fact]
    public async Task Uninstall_FromOlderService_HasEmptyLists()
    {
        _service.Respond(200, """{"success":true,"entitiesDeleted":3,"cascadedDependencies":[],"blockingDependents":[],"warnings":[]}""");

        var result = await NewClient().UninstallBlueprintAsync("acme", "System.Ai.Builder");

        Assert.Equal(3, result.EntitiesDeleted);
        Assert.False(result.DryRun);
        Assert.Empty(result.DeletedEntities);
        Assert.Empty(result.TenantOwnedKept);
    }

    private AssetServicesClient NewClient() =>
        new(new AssetServiceClientOptions { EndpointUri = _service.BaseUrl }, A.Fake<IAssetServiceClientAccessToken>());
}
