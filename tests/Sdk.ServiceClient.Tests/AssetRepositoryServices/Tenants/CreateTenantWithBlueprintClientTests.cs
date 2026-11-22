using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.System;
using Sdk.ServiceClient.Tests.BotServices;

namespace Sdk.ServiceClient.Tests.AssetRepositoryServices.Tenants;

/// <summary>
///     AB#9577: the optional <c>blueprintId</c> travels as a query parameter of the create call and only when set, so
///     the call without a blueprint is byte-identical to the one before the parameter existed.
/// </summary>
public class CreateTenantWithBlueprintClientTests : IClassFixture<LoopbackHttpService>
{
    private readonly LoopbackHttpService _service;
    private readonly AssetServicesClient _client;

    public CreateTenantWithBlueprintClientTests(LoopbackHttpService service)
    {
        _service = service;
        _service.Reset();
        _client = new AssetServicesClient(
            new AssetServiceClientOptions { EndpointUri = _service.BaseUrl, TenantId = "parent1" },
            A.Fake<IAssetServiceClientAccessToken>());
    }

    [Fact]
    public async Task CreateTenantAsync_WithABlueprint_SendsTheBlueprintId()
    {
        _service.RespondWith("/parent1/v1/tenants", 204, "");

        await _client.CreateTenantAsync("child1", "child1_db", false, "Octo.Starter-1.2.0");

        var request = _service.SingleRequest();
        Assert.StartsWith("POST /parent1/v1/tenants?", request);
        Assert.Contains("childTenantId=child1", request);
        Assert.Contains("databaseName=child1_db", request);
        Assert.Contains("blueprintId=Octo.Starter-1.2.0", request);
        Assert.DoesNotContain("scratch", request);
    }

    [Fact]
    public async Task CreateTenantAsync_WithABlueprintAndScratch_SendsBoth()
    {
        _service.RespondWith("/parent1/v1/tenants", 204, "");

        await _client.CreateTenantAsync("child1", null, true, " Octo.Starter-1.2.0 ");

        var request = _service.SingleRequest();
        Assert.Contains("scratch=true", request);
        Assert.Contains("blueprintId=Octo.Starter-1.2.0", request);
        Assert.DoesNotContain("databaseName", request);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateTenantAsync_WithoutABlueprint_IsByteIdenticalToTheCallWithoutTheParameter(
        string? blueprintId)
    {
        _service.RespondWith("/parent1/v1/tenants", 204, "");

        await _client.CreateTenantAsync("child1", "child1_db", false, blueprintId);
        await _client.CreateTenantAsync("child1", "child1_db", false);
        await _client.CreateTenantAsync("child1", "child1_db");

        var requests = _service.Requests;
        Assert.Equal(3, requests.Count);
        Assert.Equal("POST /parent1/v1/tenants?childTenantId=child1&databaseName=child1_db", requests[0]);
        Assert.Equal(requests[0], requests[1]);
        Assert.Equal(requests[0], requests[2]);
    }

    [Fact]
    public async Task CreateTenantAsync_ScratchWithoutABlueprint_IsUnchanged()
    {
        _service.RespondWith("/parent1/v1/tenants", 204, "");

        await _client.CreateTenantAsync("child1", "child1_db", true, null);

        Assert.Equal("POST /parent1/v1/tenants?childTenantId=child1&databaseName=child1_db&scratch=true",
            _service.SingleRequest());
    }

    [Fact]
    public async Task CreateTenantAsync_BlueprintFails_ThrowsWithTheServiceMessage()
    {
        // The service aborts the create transaction and answers 400 (TenantsController, AB#9577 context).
        const string body =
            "{\"statusCode\":400,\"statusDescription\":\"Bad Request\",\"message\":\"Blueprint application failed: Blueprint 'Unknown-9.9.9' not found\"}";
        _service.RespondWith("/parent1/v1/tenants", 400, body);

        var ex = await Assert.ThrowsAsync<ServiceClientResultException>(() =>
            _client.CreateTenantAsync("child1", null, false, "Unknown-9.9.9"));

        Assert.Contains("Blueprint application failed", ex.Message);
        Assert.Contains("Unknown-9.9.9", ex.Message);
        Assert.Contains("blueprintId=Unknown-9.9.9", _service.SingleRequest());
    }
}
