using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.BotServices;

namespace Sdk.ServiceClient.Tests.BotServices;

public class BotServicesClientTests
{
    [Fact]
    public void ServiceUri_WithValidEndpointUri_ReturnsCorrectUri()
    {
        var options = new BotServiceClientOptions
        {
            EndpointUri = "https://bot.example.com"
        };
        var accessToken = A.Fake<IBotServiceClientAccessToken>();
        var client = new BotServicesClient(options, accessToken);

        var uri = client.ServiceUri;

        Assert.Equal("https://bot.example.com/system/v1", uri.ToString());
    }

    [Fact]
    public void ServiceUri_WithTrailingSlash_ReturnsCorrectUri()
    {
        var options = new BotServiceClientOptions
        {
            EndpointUri = "https://bot.example.com/"
        };
        var accessToken = A.Fake<IBotServiceClientAccessToken>();
        var client = new BotServicesClient(options, accessToken);

        var uri = client.ServiceUri;

        Assert.Equal("https://bot.example.com/system/v1", uri.ToString());
    }

    [Fact]
    public void ServiceUri_NullEndpointUri_ThrowsServiceConfigurationMissingException()
    {
        var options = new BotServiceClientOptions
        {
            EndpointUri = null
        };
        var accessToken = A.Fake<IBotServiceClientAccessToken>();
        var client = new BotServicesClient(options, accessToken);

        var exception = Assert.Throws<ServiceConfigurationMissingException>(() => client.ServiceUri);
        Assert.Contains("Bot services URI", exception.Message);
    }

    [Fact]
    public void ServiceUri_EmptyEndpointUri_ThrowsServiceConfigurationMissingException()
    {
        var options = new BotServiceClientOptions
        {
            EndpointUri = ""
        };
        var accessToken = A.Fake<IBotServiceClientAccessToken>();
        var client = new BotServicesClient(options, accessToken);

        var exception = Assert.Throws<ServiceConfigurationMissingException>(() => client.ServiceUri);
        Assert.Contains("Bot services URI", exception.Message);
    }

    [Fact]
    public void ServiceUri_WhitespaceEndpointUri_ThrowsServiceConfigurationMissingException()
    {
        var options = new BotServiceClientOptions
        {
            EndpointUri = "   "
        };
        var accessToken = A.Fake<IBotServiceClientAccessToken>();
        var client = new BotServicesClient(options, accessToken);

        var exception = Assert.Throws<ServiceConfigurationMissingException>(() => client.ServiceUri);
        Assert.Contains("Bot services URI", exception.Message);
    }

    [Fact]
    public void ServiceUri_CalledMultipleTimes_ReturnsCachedValue()
    {
        var options = new BotServiceClientOptions
        {
            EndpointUri = "https://bot.example.com"
        };
        var accessToken = A.Fake<IBotServiceClientAccessToken>();
        var client = new BotServicesClient(options, accessToken);

        var uri1 = client.ServiceUri;
        var uri2 = client.ServiceUri;

        Assert.Same(uri1, uri2);
    }

    [Fact]
    public void Options_ReturnsProvidedOptions()
    {
        var options = new BotServiceClientOptions
        {
            EndpointUri = "https://bot.example.com",
            MaxTimeout = 50000
        };
        var accessToken = A.Fake<IBotServiceClientAccessToken>();
        var client = new BotServicesClient(options, accessToken);

        Assert.Same(options, client.Options);
        Assert.Equal(50000, client.Options.MaxTimeout);
    }

    [Fact]
    public void AccessToken_ReturnsProvidedAccessToken()
    {
        var options = new BotServiceClientOptions
        {
            EndpointUri = "https://bot.example.com"
        };
        var accessToken = A.Fake<IBotServiceClientAccessToken>();
        var client = new BotServicesClient(options, accessToken);

        Assert.Same(accessToken, client.AccessToken);
    }

    [Fact]
    public async Task RestoreRepositoryWithTusAsync_NonGzFile_ThrowsServiceClientException()
    {
        var tempFile = Path.GetTempFileName(); // Creates a .tmp file
        try
        {
            var options = new BotServiceClientOptions
            {
                EndpointUri = "https://bot.example.com"
            };
            var accessToken = A.Fake<IBotServiceClientAccessToken>();
            var client = new BotServicesClient(options, accessToken);

            var exception = await Assert.ThrowsAsync<ServiceClientException>(
                () => client.RestoreRepositoryWithTusAsync("tenant-1", "db-1", tempFile,
                    cancellationToken: TestContext.Current.CancellationToken));

            Assert.Contains("not a supported file", exception.Message);
            Assert.Contains(".tar.gz", exception.Message);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task RestoreRepositoryWithTusAsync_NonExistentFile_ThrowsFileNotFoundException()
    {
        var options = new BotServiceClientOptions
        {
            EndpointUri = "https://bot.example.com"
        };
        var accessToken = A.Fake<IBotServiceClientAccessToken>();
        var client = new BotServicesClient(options, accessToken);

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => client.RestoreRepositoryWithTusAsync("tenant-1", "db-1", "/nonexistent/backup.gz",
                cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DownloadDumpToFileAsync_EmptyTenantId_ThrowsArgumentException()
    {
        var options = new BotServiceClientOptions
        {
            EndpointUri = "https://bot.example.com"
        };
        var accessToken = A.Fake<IBotServiceClientAccessToken>();
        var client = new BotServicesClient(options, accessToken);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => client.DownloadDumpToFileAsync("", "job-1", "/tmp/output.gz",
                cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DownloadDumpToFileAsync_EmptyJobId_ThrowsArgumentException()
    {
        var options = new BotServiceClientOptions
        {
            EndpointUri = "https://bot.example.com"
        };
        var accessToken = A.Fake<IBotServiceClientAccessToken>();
        var client = new BotServicesClient(options, accessToken);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => client.DownloadDumpToFileAsync("tenant-1", "", "/tmp/output.gz",
                cancellationToken: TestContext.Current.CancellationToken));
    }
}
