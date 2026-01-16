using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;

namespace Sdk.ServiceClient.Tests.AssetRepositoryServices.Tenants;

public class TenantClientTests
{
    [Fact]
    public void ServiceUri_WithValidConfiguration_ReturnsCorrectUri()
    {
        // Arrange
        var options = new TenantClientOptions
        {
            EndpointUri = "https://api.example.com",
            TenantId = "test-tenant"
        };
        var accessToken = A.Fake<ITenantClientAccessToken>();
        var client = new TenantClient(options, accessToken);

        // Act
        var uri = client.ServiceUri;

        // Assert
        Assert.Equal("https://api.example.com/tenants/test-tenant/GraphQL", uri.ToString());
    }

    [Fact]
    public void ServiceUri_WithTrailingSlash_ReturnsCorrectUri()
    {
        // Arrange
        var options = new TenantClientOptions
        {
            EndpointUri = "https://api.example.com/",
            TenantId = "test-tenant"
        };
        var accessToken = A.Fake<ITenantClientAccessToken>();
        var client = new TenantClient(options, accessToken);

        // Act
        var uri = client.ServiceUri;

        // Assert
        Assert.Equal("https://api.example.com/tenants/test-tenant/GraphQL", uri.ToString());
    }

    [Fact]
    public void ServiceUri_MissingEndpointUri_ThrowsServiceConfigurationMissingException()
    {
        // Arrange
        var options = new TenantClientOptions
        {
            EndpointUri = null,
            TenantId = "test-tenant"
        };
        var accessToken = A.Fake<ITenantClientAccessToken>();
        var client = new TenantClient(options, accessToken);

        // Act & Assert
        var exception = Assert.Throws<ServiceConfigurationMissingException>(() => client.ServiceUri);
        Assert.Contains("Asset Repository Service URI", exception.Message);
    }

    [Fact]
    public void ServiceUri_EmptyEndpointUri_ThrowsServiceConfigurationMissingException()
    {
        // Arrange
        var options = new TenantClientOptions
        {
            EndpointUri = "",
            TenantId = "test-tenant"
        };
        var accessToken = A.Fake<ITenantClientAccessToken>();
        var client = new TenantClient(options, accessToken);

        // Act & Assert
        var exception = Assert.Throws<ServiceConfigurationMissingException>(() => client.ServiceUri);
        Assert.Contains("Asset Repository Service URI", exception.Message);
    }

    [Fact]
    public void ServiceUri_WhitespaceEndpointUri_ThrowsServiceConfigurationMissingException()
    {
        // Arrange
        var options = new TenantClientOptions
        {
            EndpointUri = "   ",
            TenantId = "test-tenant"
        };
        var accessToken = A.Fake<ITenantClientAccessToken>();
        var client = new TenantClient(options, accessToken);

        // Act & Assert
        var exception = Assert.Throws<ServiceConfigurationMissingException>(() => client.ServiceUri);
        Assert.Contains("Asset Repository Service URI", exception.Message);
    }

    [Fact]
    public void ServiceUri_MissingTenantId_ThrowsServiceConfigurationMissingException()
    {
        // Arrange
        var options = new TenantClientOptions
        {
            EndpointUri = "https://api.example.com",
            TenantId = null!
        };
        var accessToken = A.Fake<ITenantClientAccessToken>();
        var client = new TenantClient(options, accessToken);

        // Act & Assert
        var exception = Assert.Throws<ServiceConfigurationMissingException>(() => client.ServiceUri);
        Assert.Contains("TenantId", exception.Message);
    }

    [Fact]
    public void ServiceUri_EmptyTenantId_ThrowsServiceConfigurationMissingException()
    {
        // Arrange
        var options = new TenantClientOptions
        {
            EndpointUri = "https://api.example.com",
            TenantId = ""
        };
        var accessToken = A.Fake<ITenantClientAccessToken>();
        var client = new TenantClient(options, accessToken);

        // Act & Assert
        var exception = Assert.Throws<ServiceConfigurationMissingException>(() => client.ServiceUri);
        Assert.Contains("TenantId", exception.Message);
    }

    [Fact]
    public void ServiceUri_CalledMultipleTimes_ReturnsCachedValue()
    {
        // Arrange
        var options = new TenantClientOptions
        {
            EndpointUri = "https://api.example.com",
            TenantId = "test-tenant"
        };
        var accessToken = A.Fake<ITenantClientAccessToken>();
        var client = new TenantClient(options, accessToken);

        // Act
        var uri1 = client.ServiceUri;
        var uri2 = client.ServiceUri;

        // Assert
        Assert.Same(uri1, uri2);
    }

    [Fact]
    public void Options_ReturnsProvidedOptions()
    {
        // Arrange
        var options = new TenantClientOptions
        {
            EndpointUri = "https://api.example.com",
            TenantId = "test-tenant",
            MaxTimeout = 50000
        };
        var accessToken = A.Fake<ITenantClientAccessToken>();
        var client = new TenantClient(options, accessToken);

        // Act & Assert
        Assert.Same(options, client.Options);
        Assert.Equal("test-tenant", client.Options.TenantId);
        Assert.Equal(50000, client.Options.MaxTimeout);
    }

    [Fact]
    public void AccessToken_ReturnsProvidedAccessToken()
    {
        // Arrange
        var options = new TenantClientOptions
        {
            EndpointUri = "https://api.example.com",
            TenantId = "test-tenant"
        };
        var accessToken = A.Fake<ITenantClientAccessToken>();
        var client = new TenantClient(options, accessToken);

        // Act & Assert
        Assert.Same(accessToken, client.AccessToken);
    }
}
