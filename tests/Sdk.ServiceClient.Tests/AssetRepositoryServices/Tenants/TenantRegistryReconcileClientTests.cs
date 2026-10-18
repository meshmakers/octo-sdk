using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.System;
using Sdk.ServiceClient.Tests.BotServices;

namespace Sdk.ServiceClient.Tests.AssetRepositoryServices.Tenants;

/// <summary>
///     AB#6972: the registry reconcile call goes to the system scope (<c>POST /system/v1/tenants/reconcile-registry</c>),
///     is a dry run unless asked otherwise and returns the report with counts, inserts and conflicts.
/// </summary>
public class TenantRegistryReconcileClientTests : IClassFixture<LoopbackHttpService>
{
    private const string Report = """
        {
          "applied": true,
          "walked": 5,
          "inserted": [{"tenantId": "ec_subtenant_test", "databaseName": "ec_subtenant_test", "parentTenantId": "energycommunitymanagement"}],
          "alreadyPresent": 3,
          "conflicts": [{"tenantId": "ghost", "parentTenantId": "ecm", "reason": "database 'x' is registered for tenant 'y'"}],
          "summary": "Applied: 5 tenant(s) walked, 1 inserted, 3 already present, 1 conflict(s)"
        }
        """;

    private readonly LoopbackHttpService _service;
    private readonly AssetServicesClient _client;

    public TenantRegistryReconcileClientTests(LoopbackHttpService service)
    {
        _service = service;
        _service.Reset();
        _client = new AssetServicesClient(new AssetServiceClientOptions { EndpointUri = _service.BaseUrl },
            A.Fake<IAssetServiceClientAccessToken>());
    }

    [Fact]
    public async Task ReconcileTenantRegistryAsync_DefaultsToADryRun_OnTheSystemScope()
    {
        _service.RespondWith("/system/v1/tenants/reconcile-registry", 200, Report);

        await _client.ReconcileTenantRegistryAsync();

        Assert.Equal("POST /system/v1/tenants/reconcile-registry?apply=false", _service.SingleRequest());
    }

    [Fact]
    public async Task ReconcileTenantRegistryAsync_Apply_TravelsAsQueryAndTheReportIsParsed()
    {
        _service.RespondWith("/system/v1/tenants/reconcile-registry", 200, Report);

        var report = await _client.ReconcileTenantRegistryAsync(apply: true);

        Assert.Equal("POST /system/v1/tenants/reconcile-registry?apply=true", _service.SingleRequest());
        Assert.True(report.Applied);
        Assert.Equal(5, report.Walked);
        Assert.Equal(3, report.AlreadyPresent);
        var inserted = Assert.Single(report.Inserted);
        Assert.Equal("ec_subtenant_test", inserted.TenantId);
        Assert.Equal("energycommunitymanagement", inserted.ParentTenantId);
        var conflict = Assert.Single(report.Conflicts);
        Assert.Equal("ghost", conflict.TenantId);
        Assert.Contains("registered for tenant", conflict.Reason);
    }

    [Fact]
    public async Task ReconcileTenantRegistryAsync_AnOlderServiceWithoutTheRoute_FailsTheCall()
    {
        _service.RespondWith("/system/v1/tenants/reconcile-registry", 404, "{}");

        await Assert.ThrowsAsync<ServiceClientResultException>(() => _client.ReconcileTenantRegistryAsync());
    }
}
