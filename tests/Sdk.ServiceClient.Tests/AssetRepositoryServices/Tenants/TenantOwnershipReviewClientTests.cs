using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.System;
using Sdk.ServiceClient.Tests.BotServices;

namespace Sdk.ServiceClient.Tests.AssetRepositoryServices.Tenants;

/// <summary>
///     AB#8796: the ownership review goes to the system scope (<c>POST /system/v1/tenants/review-ownership</c>),
///     lists only unless asked to stamp and returns the report with the listed databases.
/// </summary>
public class TenantOwnershipReviewClientTests : IClassFixture<LoopbackHttpService>
{
    private const string Report = """
        {
          "systemDatabaseName": "octosystem",
          "stamp": true,
          "checked": 4,
          "owned": 1,
          "missingDatabases": 0,
          "entries": [
            {"tenantId": "legacy", "databaseName": "legacy", "issue": "Unmarked", "markerOwnerSystemDatabaseName": "octosystem", "registeringSystemDatabases": ["octosystem"], "stamped": true},
            {"tenantId": "shared", "databaseName": "shared", "issue": "RegisteredByOtherInstance", "markerOwnerSystemDatabaseName": null, "registeringSystemDatabases": ["octosystem", "octosystemdev"], "stamped": false}
          ],
          "summary": "Stamp: 4 tenant database(s) checked"
        }
        """;

    private readonly LoopbackHttpService _service;
    private readonly AssetServicesClient _client;

    public TenantOwnershipReviewClientTests(LoopbackHttpService service)
    {
        _service = service;
        _service.Reset();
        _client = new AssetServicesClient(new AssetServiceClientOptions { EndpointUri = _service.BaseUrl },
            A.Fake<IAssetServiceClientAccessToken>());
    }

    [Fact]
    public async Task ReviewTenantOwnershipAsync_ListsByDefault_OnTheSystemScope()
    {
        _service.RespondWith("/system/v1/tenants/review-ownership", 200, Report);

        await _client.ReviewTenantOwnershipAsync();

        Assert.Equal("POST /system/v1/tenants/review-ownership?stamp=false", _service.SingleRequest());
    }

    [Fact]
    public async Task ReviewTenantOwnershipAsync_Stamp_TravelsAsQueryAndTheReportIsParsed()
    {
        _service.RespondWith("/system/v1/tenants/review-ownership", 200, Report);

        var report = await _client.ReviewTenantOwnershipAsync(stamp: true);

        Assert.Equal("POST /system/v1/tenants/review-ownership?stamp=true", _service.SingleRequest());
        Assert.True(report.Stamp);
        Assert.Equal(4, report.Checked);
        Assert.Equal(2, report.Entries.Count);
        Assert.True(report.Entries[0].Stamped);
        Assert.Equal("RegisteredByOtherInstance", report.Entries[1].Issue);
        Assert.Equal(["octosystem", "octosystemdev"], report.Entries[1].RegisteringSystemDatabases);
    }

    [Fact]
    public async Task ReviewTenantOwnershipAsync_AnOlderServiceWithoutTheRoute_FailsTheCall()
    {
        _service.RespondWith("/system/v1/tenants/review-ownership", 404, "{}");

        await Assert.ThrowsAsync<ServiceClientResultException>(() => _client.ReviewTenantOwnershipAsync());
    }
}
