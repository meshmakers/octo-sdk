using System.Security.Claims;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects.ApiErrors;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.System;
using Sdk.ServiceClient.Tests.BotServices;

namespace Sdk.ServiceClient.Tests.AssetRepositoryServices.Tenants;

/// <summary>
///     AB#7192: the opt-in <c>scratch</c> flag travels as a query parameter of the create call and only when set,
///     so the unchanged call is byte-identical to what a service that predates the flag expects.
/// </summary>
public class CreateScratchTenantClientTests : IClassFixture<LoopbackHttpService>
{
    private readonly LoopbackHttpService _service;
    private readonly AssetServicesClient _client;

    public CreateScratchTenantClientTests(LoopbackHttpService service)
    {
        _service = service;
        _service.Reset();
        _client = new AssetServicesClient(new AssetServiceClientOptions { EndpointUri = _service.BaseUrl, TenantId = "parent1" },
            A.Fake<IAssetServiceClientAccessToken>());
    }

    [Fact]
    public async Task CreateTenantAsync_WithoutTheFlag_SendsNoScratchParameter()
    {
        _service.RespondWith("/parent1/v1/tenants", 204, "");

        await _client.CreateTenantAsync("child1", "child1_db");

        var request = _service.SingleRequest();
        Assert.StartsWith("POST /parent1/v1/tenants?", request);
        Assert.Contains("childTenantId=child1", request);
        Assert.DoesNotContain("scratch", request);
    }

    [Fact]
    public async Task CreateTenantAsync_ScratchFalse_SendsNoScratchParameter()
    {
        _service.RespondWith("/parent1/v1/tenants", 204, "");

        await _client.CreateTenantAsync("child1", "child1_db", false);

        Assert.DoesNotContain("scratch", _service.SingleRequest());
    }

    [Fact]
    public async Task CreateTenantAsync_ScratchTrue_SendsTheFlag()
    {
        _service.RespondWith("/parent1/v1/tenants", 204, "");

        await _client.CreateTenantAsync("child1", "child1_db", true);

        Assert.Contains("scratch=true", _service.SingleRequest());
    }

    [Fact]
    public void TenantDto_ScratchFields_DeserializeFromTheServiceShape()
    {
        const string json = """
            {"tenantId":"c1","database":"c1_db","isScratch":true,"scratchCreatorClientId":"agent",
             "scratchCreatedAtUtc":"2026-10-22T09:00:00Z","scratchAccessRevoked":true}
            """;

        var dto = System.Text.Json.JsonSerializer.Deserialize<TenantDto>(json,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(dto);
        Assert.True(dto.IsScratch);
        Assert.Equal("agent", dto.ScratchCreatorClientId);
        Assert.Equal(new DateTime(2026, 10, 22, 9, 0, 0, DateTimeKind.Utc), dto.ScratchCreatedAtUtc);
        Assert.True(dto.ScratchAccessRevoked);
    }

    [Fact]
    public void ScratchTenantMarker_Key_IsPerTenantAndNormalised()
    {
        Assert.Equal("ScratchTenant.child1", ScratchTenantMarker.GetConfigurationKey(" Child1 "));
        Assert.Throws<ArgumentException>(() => ScratchTenantMarker.GetConfigurationKey(" "));
    }

    private static ClaimsPrincipal Principal(params (string Type, string Value)[] claims) =>
        new(new ClaimsIdentity(claims.Select(c => new Claim(c.Type, c.Value)), "Bearer"));

    [Fact]
    public void TryCreateForCaller_ServiceTokenOfTheParent_BuildsTheMarker()
    {
        var reason = ScratchTenantMarker.TryCreateForCaller(
            Principal(("tenant_id", "OctoSystem"), ("client_id", "agent")), "octosystem", out var marker);

        Assert.Null(reason);
        Assert.NotNull(marker);
        Assert.Equal("octosystem", marker.ParentTenantId);
        Assert.Equal("agent", marker.CreatorClientId);
        Assert.True((DateTime.UtcNow - marker.CreatedAtUtc).TotalMinutes < 1);
    }

    [Theory]
    [InlineData("sub")]
    [InlineData("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")]
    public void TryCreateForCaller_UserToken_IsRefused(string userClaim)
    {
        var reason = ScratchTenantMarker.TryCreateForCaller(
            Principal((userClaim, "u1"), ("tenant_id", "octosystem"), ("client_id", "octo-cli")), "octosystem",
            out var marker);

        Assert.NotNull(reason);
        Assert.Contains("client-credentials", reason);
        Assert.Null(marker);
    }

    [Fact]
    public void TryCreateForCaller_TokenOfAnotherTenant_IsRefused_NamingBothTenants()
    {
        var reason = ScratchTenantMarker.TryCreateForCaller(
            Principal(("tenant_id", "acme"), ("client_id", "agent")), "octosystem", out var marker);

        Assert.Contains("'octosystem'", reason);
        Assert.Contains("'acme'", reason);
        Assert.Null(marker);
    }

    [Fact]
    public void TryCreateForCaller_TokenWithoutTenant_IsRefused()
    {
        var reason = ScratchTenantMarker.TryCreateForCaller(Principal(("client_id", "agent")), "octosystem",
            out var marker);

        Assert.Contains("<none>", reason);
        Assert.Null(marker);
    }

    [Fact]
    public void TryCreateForCaller_TokenWithoutClient_IsRefused()
    {
        var reason = ScratchTenantMarker.TryCreateForCaller(Principal(("tenant_id", "octosystem")), "octosystem",
            out var marker);

        Assert.Contains("client_id", reason);
        Assert.Null(marker);
    }

    [Fact]
    public void TryCreateForCaller_UnknownParent_IsRefused()
    {
        var reason = ScratchTenantMarker.TryCreateForCaller(
            Principal(("tenant_id", "octosystem"), ("client_id", "agent")), null, out var marker);

        Assert.NotNull(reason);
        Assert.Null(marker);
    }

    [Fact]
    public void ForbiddenErrorDto_CarriesStatus403_InTheBody()
    {
        var dto = new ForbiddenErrorDto("not allowed");

        Assert.Equal(403, dto.StatusCode);
        Assert.Equal("Forbidden", dto.StatusDescription);
        Assert.Equal("not allowed", dto.Message);
    }
}
