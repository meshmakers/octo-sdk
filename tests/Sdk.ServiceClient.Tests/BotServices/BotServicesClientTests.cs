using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.BotServices;

namespace Sdk.ServiceClient.Tests.BotServices;

public class BotServicesClientTests : IClassFixture<LoopbackHttpService>
{
    private readonly LoopbackHttpService _service;

    public BotServicesClientTests(LoopbackHttpService service)
    {
        _service = service;
        _service.Reset();
    }

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

    // ── Tenant-scoped job routes (AB#5060) ────────────────────────────────
    //
    // The five tenant-addressed job verbs moved from system/v1/jobs/…?tenantId= to
    // {tenantId}/v1/jobs/…, because only a tenant in the route value is matched against the caller's
    // token by the service's transport tenant gate. The tenant comes from the method argument, which
    // changes per call — the operations that have no tenant route must stay where they are.

    private static BotServicesClient CreateClient(string? endpointUri)
    {
        var options = new BotServiceClientOptions { EndpointUri = endpointUri };
        var accessToken = A.Fake<IBotServiceClientAccessToken>();
        return new BotServicesClient(options, accessToken);
    }

    private static string CreateTempFile(string extension)
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + extension);
        File.WriteAllText(path, "payload");
        return path;
    }

    [Fact]
    public async Task StartDumpRepositoryAsync_PostsToTheTenantRoute()
    {
        var client = CreateClient(_service.BaseUrl);

        await client.StartDumpRepositoryAsync("acme", true);

        Assert.Equal("POST /acme/v1/jobs/dump-repository?includeArchiveData=True", _service.SingleRequest());
    }

    [Fact]
    public async Task StartRunFixupScriptAsync_PostsToTheTenantRoute()
    {
        var client = CreateClient(_service.BaseUrl);

        await client.StartRunFixupScriptAsync("acme");

        Assert.Equal("POST /acme/v1/jobs/run-fixup-scripts", _service.SingleRequest());
    }

    [Fact]
    public async Task StartExportArchiveDataAsync_PostsToTheTenantRoute()
    {
        var client = CreateClient(_service.BaseUrl);

        await client.StartExportArchiveDataAsync("acme", "archive-1", null, null);

        Assert.Equal("POST /acme/v1/jobs/export-archive-data?archiveRtId=archive-1", _service.SingleRequest());
    }

    /// <summary>
    ///     The upload and the job start live in one method body and now address the <b>same</b> tenant
    ///     route, so both are pinned here — including their order, since the job start must carry the
    ///     id the upload returned.
    /// </summary>
    /// <remarks>
    ///     The sink used to be <c>system/v1/tus-upload</c> with the tenant as upload metadata, which
    ///     the service's transport gate never saw and which bound nothing: the file was stored flat
    ///     under its tus file id and no consumer read the metadata back. Since AB#5060 the upload is
    ///     tenant-routed and stored under the tenant's own directory. A regression to the system path
    ///     would be a silently ungated upload, which is why the URL shape is asserted rather than
    ///     just the outcome.
    /// </remarks>
    [Fact]
    public async Task RestoreRepositoryWithTusAsync_UploadsAndStartsTheJobOnTheTenantRoute()
    {
        var client = CreateClient(_service.BaseUrl);
        var backupFile = CreateTempFile(".gz");

        try
        {
            await client.RestoreRepositoryWithTusAsync("acme", "db-1", backupFile,
                cancellationToken: TestContext.Current.CancellationToken);
        }
        finally
        {
            File.Delete(backupFile);
        }

        Assert.Equal(new[]
        {
            "POST /acme/v1/tus-upload",
            $"HEAD /acme/v1/tus-upload/{LoopbackHttpService.TusFileId}",
            $"PATCH /acme/v1/tus-upload/{LoopbackHttpService.TusFileId}",
            $"POST /acme/v1/jobs/restore-from-upload?tusFileId={LoopbackHttpService.TusFileId}" +
            "&databaseName=db-1&restoreArchiveData=False"
        }, _service.Requests);
    }

    /// <inheritdoc cref="RestoreRepositoryWithTusAsync_UploadsAndStartsTheJobOnTheTenantRoute" />
    [Fact]
    public async Task StartImportArchiveDataWithTusAsync_UploadsAndStartsTheJobOnTheTenantRoute()
    {
        var client = CreateClient(_service.BaseUrl);
        var exportFile = CreateTempFile(".zip");

        try
        {
            await client.StartImportArchiveDataWithTusAsync("acme", "archive-1", exportFile,
                ArchiveImportMode.Upsert,
                cancellationToken: TestContext.Current.CancellationToken);
        }
        finally
        {
            File.Delete(exportFile);
        }

        Assert.Equal(new[]
        {
            "POST /acme/v1/tus-upload",
            $"HEAD /acme/v1/tus-upload/{LoopbackHttpService.TusFileId}",
            $"PATCH /acme/v1/tus-upload/{LoopbackHttpService.TusFileId}",
            "POST /acme/v1/jobs/import-archive-data-from-upload?archiveRtId=archive-1" +
            $"&tusFileId={LoopbackHttpService.TusFileId}&mode=Upsert"
        }, _service.Requests);
    }

    /// <summary>
    ///     The tenant segment is built from a caller-supplied argument, and <see cref="Uri" /> normalises
    ///     dot segments — an unescaped value could walk straight back out into <c>system/v1</c>, which is
    ///     the scope the route exists to establish.
    /// </summary>
    [Fact]
    public async Task StartDumpRepositoryAsync_TenantIdWithPathSeparators_CannotEscapeTheTenantSegment()
    {
        var client = CreateClient(_service.BaseUrl);

        await client.StartDumpRepositoryAsync("../system");

        Assert.StartsWith("POST /..%2Fsystem/v1/jobs/dump-repository", _service.SingleRequest());
    }

    // ── Operations without a tenant route stay system-scoped ──────────────

    [Fact]
    public void ServiceUri_StaysSystemScoped()
    {
        var client = CreateClient("https://bot.example.com");

        Assert.Equal("https://bot.example.com/system/v1", client.ServiceUri.ToString());
    }

    [Fact]
    public async Task GetImportJobStatus_StaysOnTheSystemRoute()
    {
        var client = CreateClient(_service.BaseUrl);

        await client.GetImportJobStatus("job-1");

        Assert.Equal("GET /system/v1/jobs?id=job-1", _service.SingleRequest());
    }

    [Fact]
    public async Task DownloadExportRtResultAsync_StaysOnTheSystemRoute()
    {
        var client = CreateClient(_service.BaseUrl);

#pragma warning disable CS0618 // the obsolete overload must keep routing correctly until it is removed
        await client.DownloadExportRtResultAsync("acme", "job-1");
#pragma warning restore CS0618

        Assert.Equal("GET /system/v1/jobs/download?tenantId=acme&id=job-1", _service.SingleRequest());
    }

    [Fact]
    public async Task DownloadDumpToFileAsync_StaysOnTheSystemRoute()
    {
        var client = CreateClient(_service.BaseUrl);
        var outputFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        try
        {
            await client.DownloadDumpToFileAsync("acme", "job-1", outputFile,
                cancellationToken: TestContext.Current.CancellationToken);
        }
        finally
        {
            File.Delete(outputFile);
        }

        Assert.Equal("GET /system/v1/jobs/download?tenantId=acme&id=job-1", _service.SingleRequest());
    }

    [Fact]
    public async Task ReconfigureLogLevelAsync_StaysOnTheSystemRoute()
    {
        var client = CreateClient(_service.BaseUrl);

        await client.ReconfigureLogLevelAsync("Microsoft.*", LogLevelDto.Debug, LogLevelDto.Error);

        Assert.Equal(
            "POST /system/v1/diagnostics/reconfigureLogLevel?loggerName=Microsoft.*&minLogLevel=Debug&maxLogLevel=Error",
            _service.SingleRequest());
    }
}
