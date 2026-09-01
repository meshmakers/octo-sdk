using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;

namespace Sdk.ServiceClient.Tests.CommunicationControllerServices;

public class CommunicationServicesClientTests
{
    private static CommunicationServicesClient CreateClient(string? endpointUri, string? tenantId)
    {
        var options = new CommunicationServiceClientOptions
        {
            EndpointUri = endpointUri,
            TenantId = tenantId
        };
        var accessToken = A.Fake<ICommunicationServiceClientAccessToken>();
        return new CommunicationServicesClient(options, accessToken);
    }

    [Fact]
    public void ServiceUri_WithTenantId_ReturnsTenantScopedUri()
    {
        var client = CreateClient("https://comm.example.com", "acme");

        Assert.Equal("https://comm.example.com/acme/v1", client.ServiceUri.ToString());
    }

    [Fact]
    public void ServiceUri_WithTrailingSlash_ReturnsTenantScopedUri()
    {
        var client = CreateClient("https://comm.example.com/", "acme");

        Assert.Equal("https://comm.example.com/acme/v1", client.ServiceUri.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ServiceUri_BlankTenantId_ThrowsServiceConfigurationMissingException(string? tenantId)
    {
        var client = CreateClient("https://comm.example.com", tenantId);

        var exception = Assert.Throws<ServiceConfigurationMissingException>(() => client.ServiceUri);
        Assert.Contains("tenant ID", exception.Message);
    }

    [Fact]
    public void ServiceUri_MissingEndpointUri_ThrowsServiceConfigurationMissingException()
    {
        var client = CreateClient(null, "acme");

        var exception = Assert.Throws<ServiceConfigurationMissingException>(() => client.ServiceUri);
        Assert.Contains("URI is missing", exception.Message);
    }

    // ── Service account secret rotation (AB#5032 / AB#5048) ───────────────

    /// <summary>
    ///     The rotation invalidates the old credential the moment it reaches the controller, so a
    ///     blank adapter id must never turn into a request. It is rejected before anything is sent —
    ///     an empty URL segment would otherwise post to the tenant's adapter collection route.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task RotateServiceAccountSecret_BlankAdapterRtId_ThrowsBeforeSendingARequest(string? adapterRtId)
    {
        var client = CreateClient("https://comm.example.com", "acme");

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => client.RotateServiceAccountSecretAsync(adapterRtId!));

        Assert.Equal("adapterRtId", exception.ParamName);
    }
}
