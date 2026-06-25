using System.Net.Http.Headers;
using BirdMessenger;
using BirdMessenger.Collections;
using Meshmakers.Common.Shared;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Meshmakers.Octo.Sdk.ServiceClient.BotServices;

/// <summary>
///     Implementation of the client proxy for bot services.
/// </summary>
public class BotServicesClient : ServiceClient, IBotServicesClient
{
    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="serviceClientOptions">Options for configuration of the client proxy.</param>
    /// <param name="botAccessToken">The access token management object</param>
    public BotServicesClient(IOptions<BotServiceClientOptions> serviceClientOptions,
        IBotServiceClientAccessToken botAccessToken)
        : this(serviceClientOptions.Value, botAccessToken)
    {
    }

    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="serviceClientOptions">Options for configuration of the client proxy.</param>
    /// <param name="botAccessToken">The access token management object</param>
    public BotServicesClient(BotServiceClientOptions serviceClientOptions,
        IBotServiceClientAccessToken botAccessToken)
        : base(serviceClientOptions, botAccessToken)
    {
    }

    /// <inheritdoc />
    public async Task<JobDto> GetImportJobStatus(string id)
    {
        ArgumentValidation.ValidateString(nameof(id), id);

        var request = new RestRequest("jobs");
        request.AddQueryParameter("id", id);

        var response = await Client.ExecuteAsync<JobDto>(request);
        ValidateResponse(response);

        return response.Data!;
    }

    /// <inheritdoc />
    public async Task<byte[]> DownloadExportRtResultAsync(string tenantId, string id)
    {
        ArgumentValidation.ValidateString(nameof(id), id);

        var request = new RestRequest("jobs/download");
        request.AddQueryParameter("tenantId", tenantId);
        request.AddQueryParameter("id", id);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);

        return response.RawBytes!;
    }

    /// <inheritdoc />
    public async Task<JobResponseDto> StartRunFixupScriptAsync(string tenantId)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);

        var request = new RestRequest("jobs/run-fixup-scripts", Method.Post);
        request.AddQueryParameter("tenantId", tenantId);

        var response = await Client.ExecuteAsync<JobResponseDto>(request);
        ValidateResponse(response);

        if (response.Data == null)
        {
            throw ServiceClientResultException.NoDataReturned();
        }

        return response.Data;
    }

    /// <inheritdoc />
    public async Task<JobResponseDto> StartDumpRepositoryAsync(string tenantId)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);

        var request = new RestRequest("jobs/dump-repository", Method.Post);
        request.AddQueryParameter("tenantId", tenantId);

        var response = await Client.ExecuteAsync<JobResponseDto>(request);
        ValidateResponse(response);

        if (response.Data == null)
        {
            throw ServiceClientResultException.NoDataReturned();
        }

        return response.Data;
    }

    /// <inheritdoc />
    public async Task ReconfigureLogLevelAsync(string loggerName, LogLevelDto minLogLevel, LogLevelDto maxLogLevel)
    {
        var request = new RestRequest("diagnostics/reconfigureLogLevel", Method.Post);
        request.AddQueryParameter("loggerName", loggerName);
        request.AddQueryParameter("minLogLevel", minLogLevel);
        request.AddQueryParameter("maxLogLevel", maxLogLevel);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task<JobResponseDto> RestoreRepositoryWithTusAsync(string tenantId, string databaseName,
        string filePath,
        string? oldDatabaseName = null,
        Action<double>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);
        ArgumentValidation.ValidateString(nameof(databaseName), databaseName);
        ArgumentValidation.ValidateExistingFile(nameof(filePath), filePath);

        if (!Path.GetExtension(filePath).Equals(".gz", StringComparison.OrdinalIgnoreCase))
        {
            throw new ServiceClientException($"'{filePath}' is not a supported file. Only .tar.gz files are supported.");
        }

        // Build the tus endpoint URL
        var tusEndpointUrl = new Uri(new Uri(Options.EndpointUri!), "system/v1/tus-upload");

        var fileInfo = new FileInfo(filePath);
        var metadata = new MetadataCollection
        {
            ["tenantId"] = tenantId,
            ["databaseName"] = databaseName,
            ["fileName"] = fileInfo.Name,
            ["contentType"] = MimeTypes.MimeTypeGzip
        };

        if (!string.IsNullOrWhiteSpace(oldDatabaseName))
        {
            metadata["oldDatabaseName"] = oldDatabaseName!;
        }

        // Create HttpClient with auth header
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("bearer", AccessToken.AccessToken);

        // Create upload on server
        var createOption = new TusCreateRequestOption
        {
            Endpoint = tusEndpointUrl,
            Metadata = metadata,
            UploadLength = fileInfo.Length
        };

        var createResponse = await httpClient.TusCreateAsync(createOption, cancellationToken);

        // Upload file with progress - use adaptive buffer size to minimize round-trips
        const long smallFileThreshold = 300L * 1024 * 1024;
        var uploadBufferSize = fileInfo.Length <= smallFileThreshold
            ? (uint)Math.Min(fileInfo.Length, uint.MaxValue)
            : 100u * 1024 * 1024;

        using var fileStream = fileInfo.OpenRead();
        var patchOption = new TusPatchRequestOption
        {
            FileLocation = createResponse.FileLocation,
            Stream = fileStream,
            UploadBufferSize = uploadBufferSize,
            OnProgressAsync = ctx =>
            {
                if (ctx.TotalSize > 0)
                {
                    var progress = (double)ctx.UploadedSize / ctx.TotalSize.Value;
                    progressCallback?.Invoke(progress);
                }

                return Task.CompletedTask;
            }
        };

        await httpClient.TusPatchAsync(patchOption, cancellationToken);

        // Extract tus file ID from the URL
        var tusFileId = createResponse.FileLocation.Segments.Last();

        // Start the restore job via REST
        var request = new RestRequest("jobs/restore-from-upload", Method.Post);
        request.AddQueryParameter("tusFileId", tusFileId);
        request.AddQueryParameter("tenantId", tenantId);
        request.AddQueryParameter("databaseName", databaseName);
        if (!string.IsNullOrWhiteSpace(oldDatabaseName))
        {
            request.AddQueryParameter("oldDatabaseName", oldDatabaseName);
        }

        var response = await Client.ExecuteAsync<JobResponseDto>(request);
        ValidateResponse(response);

        if (response.Data == null)
        {
            throw ServiceClientResultException.NoDataReturned();
        }

        return response.Data;
    }

    /// <inheritdoc />
    public async Task DownloadDumpToFileAsync(string tenantId, string jobId, string outputFilePath,
        Action<long>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);
        ArgumentValidation.ValidateString(nameof(jobId), jobId);
        ArgumentValidation.ValidateString(nameof(outputFilePath), outputFilePath);

        var downloadUrl = ServiceUri.Append("jobs", "download");
        var uriBuilder = new UriBuilder(downloadUrl);
        uriBuilder.Query = $"tenantId={Uri.EscapeDataString(tenantId)}&id={Uri.EscapeDataString(jobId)}";

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("bearer", AccessToken.AccessToken);
        httpClient.Timeout = TimeSpan.FromHours(2);

        using var response = await httpClient.GetAsync(uriBuilder.Uri, HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        using var contentStream = await response.Content.ReadAsStreamAsync();
        using var fileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: 81920, useAsync: true);

        var buffer = new byte[81920];
        long totalBytesRead = 0;
        int bytesRead;

        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
            totalBytesRead += bytesRead;
            progressCallback?.Invoke(totalBytesRead);
        }
    }

    /// <inheritdoc />
    public async Task<JobResponseDto> StartExportArchiveDataAsync(string tenantId, string archiveRtId,
        DateTime? fromUtc, DateTime? toUtc)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);
        ArgumentValidation.ValidateString(nameof(archiveRtId), archiveRtId);

        var request = new RestRequest("jobs/export-archive-data", Method.Post);
        request.AddQueryParameter("tenantId", tenantId);
        request.AddQueryParameter("archiveRtId", archiveRtId);
        if (fromUtc.HasValue)
        {
            request.AddQueryParameter("fromUtc", fromUtc.Value.ToUniversalTime().ToString("O"));
        }

        if (toUtc.HasValue)
        {
            request.AddQueryParameter("toUtc", toUtc.Value.ToUniversalTime().ToString("O"));
        }

        var response = await Client.ExecuteAsync<JobResponseDto>(request);
        ValidateResponse(response);

        if (response.Data == null)
        {
            throw ServiceClientResultException.NoDataReturned();
        }

        return response.Data;
    }

    /// <inheritdoc />
    public async Task<JobResponseDto> StartImportArchiveDataWithTusAsync(string tenantId, string archiveRtId,
        string filePath,
        ArchiveImportMode mode,
        Action<double>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);
        ArgumentValidation.ValidateString(nameof(archiveRtId), archiveRtId);
        ArgumentValidation.ValidateExistingFile(nameof(filePath), filePath);

        if (!Path.GetExtension(filePath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
        {
            throw new ServiceClientException($"'{filePath}' is not a supported file. Only .zip files are supported.");
        }

        // Build the tus endpoint URL (same upload surface as repository restore).
        var tusEndpointUrl = new Uri(new Uri(Options.EndpointUri!), "system/v1/tus-upload");

        var fileInfo = new FileInfo(filePath);
        var metadata = new MetadataCollection
        {
            ["tenantId"] = tenantId,
            ["archiveRtId"] = archiveRtId,
            ["fileName"] = fileInfo.Name,
            ["contentType"] = MimeTypes.MimeTypeZip
        };

        // Create HttpClient with auth header
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("bearer", AccessToken.AccessToken);

        // Create upload on server
        var createOption = new TusCreateRequestOption
        {
            Endpoint = tusEndpointUrl,
            Metadata = metadata,
            UploadLength = fileInfo.Length
        };

        var createResponse = await httpClient.TusCreateAsync(createOption, cancellationToken);

        // Upload file with progress - use adaptive buffer size to minimize round-trips
        const long smallFileThreshold = 300L * 1024 * 1024;
        var uploadBufferSize = fileInfo.Length <= smallFileThreshold
            ? (uint)Math.Min(fileInfo.Length, uint.MaxValue)
            : 100u * 1024 * 1024;

        using var fileStream = fileInfo.OpenRead();
        var patchOption = new TusPatchRequestOption
        {
            FileLocation = createResponse.FileLocation,
            Stream = fileStream,
            UploadBufferSize = uploadBufferSize,
            OnProgressAsync = ctx =>
            {
                if (ctx.TotalSize > 0)
                {
                    var progress = (double)ctx.UploadedSize / ctx.TotalSize.Value;
                    progressCallback?.Invoke(progress);
                }

                return Task.CompletedTask;
            }
        };

        await httpClient.TusPatchAsync(patchOption, cancellationToken);

        // Extract tus file ID from the URL
        var tusFileId = createResponse.FileLocation.Segments.Last();

        // Start the import job via REST
        var request = new RestRequest("jobs/import-archive-data-from-upload", Method.Post);
        request.AddQueryParameter("tenantId", tenantId);
        request.AddQueryParameter("archiveRtId", archiveRtId);
        request.AddQueryParameter("tusFileId", tusFileId);
        request.AddQueryParameter("mode", mode.ToString());

        var response = await Client.ExecuteAsync<JobResponseDto>(request);
        ValidateResponse(response);

        if (response.Data == null)
        {
            throw ServiceClientResultException.NoDataReturned();
        }

        return response.Data;
    }

    /// <inheritdoc />
    protected override Uri BuildServiceUri()
    {
        if (string.IsNullOrWhiteSpace(Options.EndpointUri))
        {
            throw new ServiceConfigurationMissingException("Bot services URI is missing");
        }

        return new Uri(Options.EndpointUri).Append("system").Append("v1");
    }
}