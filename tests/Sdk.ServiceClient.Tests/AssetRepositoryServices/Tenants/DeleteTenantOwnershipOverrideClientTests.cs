using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.System;
using Sdk.ServiceClient.Tests.BotServices;

namespace Sdk.ServiceClient.Tests.AssetRepositoryServices.Tenants;

/// <summary>
///     AB#8807: the deliberate ownership override of the delete travels as a query parameter and only when set, so a
///     plain delete is the same request as before.
/// </summary>
public class DeleteTenantOwnershipOverrideClientTests : IClassFixture<LoopbackHttpService>
{
    private readonly LoopbackHttpService _service;
    private readonly AssetServicesClient _client;

    public DeleteTenantOwnershipOverrideClientTests(LoopbackHttpService service)
    {
        _service = service;
        _service.Reset();
        _client = new AssetServicesClient(new AssetServiceClientOptions { EndpointUri = _service.BaseUrl, TenantId = "parent1" },
            A.Fake<IAssetServiceClientAccessToken>());
    }

    [Fact]
    public async Task DeleteTenantAsync_WithoutTheOverride_SendsNoOverrideParameter()
    {
        _service.RespondWith("/parent1/v1/tenants", 200, "");

        await _client.DeleteTenantAsync("child1");

        Assert.Equal("DELETE /parent1/v1/tenants?childTenantId=child1", _service.SingleRequest());
    }

    [Fact]
    public async Task DeleteTenantAsync_OverrideFalse_SendsNoOverrideParameter()
    {
        _service.RespondWith("/parent1/v1/tenants", 200, "");

        await _client.DeleteTenantAsync("child1", false);

        Assert.Equal("DELETE /parent1/v1/tenants?childTenantId=child1", _service.SingleRequest());
    }

    [Fact]
    public async Task DeleteTenantAsync_OverrideTrue_SendsTheOverride()
    {
        _service.RespondWith("/parent1/v1/tenants", 200, "");

        await _client.DeleteTenantAsync("child1", true);

        Assert.Equal("DELETE /parent1/v1/tenants?childTenantId=child1&overrideForeignOwnership=true",
            _service.SingleRequest());
    }

    [Fact]
    public async Task DeleteTenantAsync_TheUniformConflict_FailsTheCall()
    {
        _service.RespondWith("/parent1/v1/tenants", 409, """{"message":"Database name 'child1' is not available."}""");

        await Assert.ThrowsAnyAsync<ServiceClientResultException>(() => _client.DeleteTenantAsync("child1"));
    }
}
