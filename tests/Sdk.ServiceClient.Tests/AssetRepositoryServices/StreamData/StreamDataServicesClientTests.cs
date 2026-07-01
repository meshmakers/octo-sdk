using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.StreamData;

namespace Sdk.ServiceClient.Tests.AssetRepositoryServices.StreamData;

public class StreamDataServicesClientTests
{
    private static StreamDataServicesClient CreateClient(string? endpointUri, string? tenantId)
    {
        var options = new StreamDataServiceClientOptions
        {
            EndpointUri = endpointUri,
            TenantId = tenantId
        };
        var accessToken = A.Fake<IStreamDataServiceClientAccessToken>();
        return new StreamDataServicesClient(options, accessToken);
    }

    [Fact]
    public void ServiceUri_WithTenantId_ReturnsTenantScopedUri()
    {
        var client = CreateClient("https://asset.example.com", "acme");

        Assert.Equal("https://asset.example.com/acme/v1", client.ServiceUri.ToString());
    }

    [Fact]
    public void ServiceUri_WithTrailingSlash_ReturnsTenantScopedUri()
    {
        var client = CreateClient("https://asset.example.com/", "acme");

        Assert.Equal("https://asset.example.com/acme/v1", client.ServiceUri.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ServiceUri_BlankTenantId_ThrowsServiceConfigurationMissingException(string? tenantId)
    {
        var client = CreateClient("https://asset.example.com", tenantId);

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
