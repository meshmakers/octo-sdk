using Meshmakers.Octo.Sdk.ServiceClient;

namespace Sdk.ServiceClient.Tests;

public class UriExtensionsTests
{
    [Fact]
    public void Append_SinglePath_AppendsCorrectly()
    {
        // Arrange
        var baseUri = new Uri("https://api.example.com");

        // Act
        var result = baseUri.Append("tenants");

        // Assert
        Assert.Equal("https://api.example.com/tenants", result.ToString());
    }

    [Fact]
    public void Append_MultiplePaths_AppendsAllCorrectly()
    {
        // Arrange
        var baseUri = new Uri("https://api.example.com");

        // Act
        var result = baseUri.Append("tenants", "123", "graphql");

        // Assert
        Assert.Equal("https://api.example.com/tenants/123/graphql", result.ToString());
    }

    [Fact]
    public void Append_BaseUriWithTrailingSlash_RemovesDuplicateSlash()
    {
        // Arrange
        var baseUri = new Uri("https://api.example.com/");

        // Act
        var result = baseUri.Append("tenants");

        // Assert
        Assert.Equal("https://api.example.com/tenants", result.ToString());
    }

    [Fact]
    public void Append_PathWithLeadingSlash_RemovesDuplicateSlash()
    {
        // Arrange
        var baseUri = new Uri("https://api.example.com");

        // Act
        var result = baseUri.Append("/tenants");

        // Assert
        Assert.Equal("https://api.example.com/tenants", result.ToString());
    }

    [Fact]
    public void Append_BothWithSlashes_RemovesDuplicateSlashes()
    {
        // Arrange
        var baseUri = new Uri("https://api.example.com/");

        // Act
        var result = baseUri.Append("/tenants/", "/123/");

        // Assert
        Assert.Equal("https://api.example.com/tenants/123/", result.ToString());
    }

    [Fact]
    public void Append_EmptyPath_ReturnsBaseUri()
    {
        // Arrange
        var baseUri = new Uri("https://api.example.com");

        // Act
        var result = baseUri.Append("");

        // Assert
        Assert.Equal("https://api.example.com/", result.ToString());
    }

    [Fact]
    public void Append_NoPaths_ReturnsBaseUri()
    {
        // Arrange
        var baseUri = new Uri("https://api.example.com");

        // Act
        var result = baseUri.Append();

        // Assert
        Assert.Equal("https://api.example.com/", result.ToString());
    }

    [Fact]
    public void Append_WithQueryString_PreservesQueryString()
    {
        // Arrange
        var baseUri = new Uri("https://api.example.com?version=1");

        // Act
        var result = baseUri.Append("tenants");

        // Assert
        Assert.Contains("tenants", result.ToString());
    }

    [Fact]
    public void Append_ChainedCalls_WorksCorrectly()
    {
        // Arrange
        var baseUri = new Uri("https://api.example.com");

        // Act
        var result = baseUri
            .Append("api")
            .Append("v1")
            .Append("tenants");

        // Assert
        Assert.Equal("https://api.example.com/api/v1/tenants", result.ToString());
    }
}
