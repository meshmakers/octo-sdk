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
/// <remarks>
///     <para>
///         🔴 <b>The base URI stays system-scoped; five job verbs leave it (AB#5060).</b> Dump,
///         restore-from-upload, archive export, archive import and the fixup run used to address their
///         tenant through a <c>?tenantId=</c> query parameter on <c>system/v1/jobs/…</c>. The service's
///         transport tenant gate reads the tenant from the <b>route value</b>, so that form was never
///         matched against the caller's <c>tenant_id</c> claim — a token issued for one tenant could dump
///         or restore another. Those five now build an absolute <c>{tenantId}/v1/jobs/…</c> URL from
///         their own <c>tenantId</c> argument (see <see cref="BuildTenantJobUri" />), the same way the
///         tus calls below build their absolute upload URL.
///     </para>
///     <para>
///         <b>The tenant is per call, not per client, on purpose.</b> It is the first argument of every
///         one of those methods and routinely differs from the caller's own tenant — backing up a child
///         tenant as its parent's administrator is exactly what the tenant routes were added for. An
///         ambient tenant on the options could not express it: <see cref="ServiceClient.ServiceUri" /> is
///         built once and cached, and <c>octo-cli</c> holds this client as a singleton. Keeping the
///         tenant in the argument also leaves every public signature untouched.
///     </para>
///     <para>
///         <b>What deliberately stays on <c>system/v1</c>:</b> job status, job download (both the buffered
///         and the streaming variant) and the diagnostics log-level verb — they act on a job instance or
///         on the service process, not on a tenant, and have no tenant route. So does the tus upload sink:
///         the service stores the upload flat under its tus file id, and the tenant-carrying, gated
///         decision is the restore / import call that consumes it. Both tus methods below build the
///         system upload URL and a tenant job URL in the same body — they are not interchangeable.
///     </para>
/// </remarks>
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

    /// <summary>
    ///     Builds the absolute URL of a tenant-scoped job action, <c>{endpoint}/{tenantId}/v1/jobs/{action}</c>
    ///     (AB#5060). RestSharp uses an absolute resource URL as-is instead of combining it with the
    ///     client's base URI, which stays system-scoped for the operations that have no tenant route.
    /// </summary>
    /// <param name="tenantId">The tenant to address — the target of the call, not the caller's own.</param>
    /// <param name="action">The job action, e.g. <c>dump-repository</c>.</param>
    private Uri BuildTenantJobUri(string tenantId, string action)
    {
        if (string.IsNullOrWhiteSpace(Options.EndpointUri))
        {
            throw new ServiceConfigurationMissingException("Bot services URI is missing");
        }

        // Escaped: the segment comes from a caller-supplied argument, and Uri normalises dot segments,
        // so an unescaped value could walk out of the tenant scope the route is there to establish.
        return new Uri(Options.EndpointUri!).Append(Uri.EscapeDataString(tenantId), "v1", "jobs", action);
    }

    /// <inheritdoc />
    public async Task<JobDto> GetImportJobStatus(string id)
    {
        ArgumentValidation.ValidateString(nameof(id), id);

        // Stays on the system API (AB#5060): addresses a job instance, not a tenant.
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

        // Stays on the system API (AB#5060): the download hands out the produced artifact of a job and
        // has no tenant route.
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

        var request = new RestRequest(BuildTenantJobUri(tenantId, "run-fixup-scripts"), Method.Post);

        var response = await Client.ExecuteAsync<JobResponseDto>(request);
        ValidateResponse(response);

        if (response.Data == null)
        {
            throw ServiceClientResultException.NoDataReturned();
        }

        return response.Data;
    }

    /// <inheritdoc />
    public async Task<JobResponseDto> StartDumpRepositoryAsync(string tenantId, bool includeArchiveData = false)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);

        var request = new RestRequest(BuildTenantJobUri(tenantId, "dump-repository"), Method.Post);
        request.AddQueryParameter("includeArchiveData", includeArchiveData);

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
        // Stays on the system API (AB#5060): the DiagnosticsController acts on the service process,
        // not on a tenant.
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
        bool restoreArchiveData = false,
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

        // Build the tus endpoint URL. The upload sink is system-scoped by design (AB#5060): the service
        // stores the file flat under its tus file id, so the tenant-carrying decision is the restore call
        // below, not the upload.
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
        // Tenant-scoped route (AB#5060) — unlike the upload above, which is system-scoped by design.
        var request = new RestRequest(BuildTenantJobUri(tenantId, "restore-from-upload"), Method.Post);
        request.AddQueryParameter("tusFileId", tusFileId);
        request.AddQueryParameter("databaseName", databaseName);
        if (!string.IsNullOrWhiteSpace(oldDatabaseName))
        {
            request.AddQueryParameter("oldDatabaseName", oldDatabaseName);
        }

        request.AddQueryParameter("restoreArchiveData", restoreArchiveData);

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

        // Stays on the system API (AB#5060): the download hands out the produced artifact of a job and
        // has no tenant route. ServiceUri is the system base URI.
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

        var request = new RestRequest(BuildTenantJobUri(tenantId, "export-archive-data"), Method.Post);
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
        // Tenant-scoped route (AB#5060) — unlike the upload above, which is system-scoped by design.
        var request = new RestRequest(BuildTenantJobUri(tenantId, "import-archive-data-from-upload"),
            Method.Post);
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
    /// <remarks>
    ///     Stays system-scoped (AB#5060). It is the base URI of the operations that have no tenant route —
    ///     job status, job download, diagnostics and the tus upload sink. The tenant-addressed job verbs
    ///     do not use it; they build their URL per call from their own <c>tenantId</c> argument, because
    ///     this value is computed once and cached for the lifetime of the client.
    /// </remarks>
    protected override Uri BuildServiceUri()
    {
        if (string.IsNullOrWhiteSpace(Options.EndpointUri))
        {
            throw new ServiceConfigurationMissingException("Bot services URI is missing");
        }

        return new Uri(Options.EndpointUri).Append("system").Append("v1");
    }
}