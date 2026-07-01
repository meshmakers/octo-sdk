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
}
