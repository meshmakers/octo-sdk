using System.Net;
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

    /// <summary>
    ///     Builds a tenant-addressed secrets admin URL (<c>{tenantId}/v1/secrets/{segments}</c>, AB#5544).
    /// </summary>
    /// <param name="tenantId">The tenant (caller-supplied, escaped).</param>
    /// <param name="segments">Path segments below <c>secrets</c>; caller-supplied ones must be escaped.</param>
    private Uri BuildTenantSecretsUri(string tenantId, params string[] segments)
    {
        if (string.IsNullOrWhiteSpace(Options.EndpointUri))
        {
            throw new ServiceConfigurationMissingException("Bot services URI is missing");
        }

        // Escaped for the same reason as BuildTenantJobUri.
        return new Uri(Options.EndpointUri!).Append([Uri.EscapeDataString(tenantId), "v1", "secrets", .. segments]);
    }

    /// <summary>
    ///     Builds the tus upload endpoint for <paramref name="tenantId" />.
    /// </summary>
    /// <remarks>
    ///     🔴 <b>The sink is tenant-routed (AB#5060).</b> It used to be <c>system/v1/tus-upload</c>
    ///     with the tenant sent as an upload-metadata field — which the service's transport gate
    ///     never saw, because the gate reads the route value, and which bound nothing, because the
    ///     file was stored flat under its tus file id and no consumer read the field back. The
    ///     service now stores uploads under the tenant's own directory, so the restore call can only
    ///     resolve a file staged for the tenant it restores.
    /// </remarks>
    /// <param name="tenantId">The tenant the upload is staged for.</param>
    private Uri BuildTusUploadUri(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(Options.EndpointUri))
        {
            throw new ServiceConfigurationMissingException("Bot services URI is missing");
        }

        // Escaped for the same reason as BuildTenantJobUri: the segment is caller-supplied and Uri
        // normalises dot segments.
        return new Uri(Options.EndpointUri!).Append(Uri.EscapeDataString(tenantId), "v1", "tus-upload");
    }

    /// <summary>
    ///     Creates a tus upload and sends <paramref name="fileInfo" /> to it in parts of at most
    ///     <see cref="BotServiceClientOptions.TusPartSizeBytes" /> (AB#6786), returning the tus file id.
    /// </summary>
    /// <remarks>
    ///     One request carries the whole file only if the file is not larger than a part. Before AB#6786 the file went
    ///     out in a single PATCH, which the ingress of the managed environments refuses above 100m with a 413 that
    ///     the client did not report.
    /// </remarks>
    private async Task<string> UploadWithTusAsync(Uri tusEndpointUrl, MetadataCollection metadata,
        FileInfo fileInfo, Action<double>? progressCallback, CancellationToken cancellationToken)
    {
        var partSizeBytes = (Options as BotServiceClientOptions)?.TusPartSizeBytes ??
                            BotServiceClientOptions.DefaultTusPartSizeBytes;

        // Create HttpClient with auth header. The time limit is per part (TusUploader), not for the whole file.
        using var httpClient = ServerCertificateTrust.CreateHttpClient();
        httpClient.Timeout = Timeout.InfiniteTimeSpan;
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

        await TusUploader.UploadAsync(httpClient, createResponse.FileLocation, fileInfo.FullName, partSizeBytes,
            progressCallback, cancellationToken: cancellationToken);

        // Extract tus file ID from the URL
        return createResponse.FileLocation.Segments.Last();
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

        // Two artifact shapes exist since AB#4231: the plain mongodump '.tar.gz' and the '.octobak.zip'
        // container that also carries the CrateDB archive rows. The restore job auto-detects the shape
        // by content (manifest.json), so the client only rejects what can be neither (AB#5141).
        var extension = Path.GetExtension(filePath);
        var isZip = extension.Equals(".zip", StringComparison.OrdinalIgnoreCase);
        if (!isZip && !extension.Equals(".gz", StringComparison.OrdinalIgnoreCase))
        {
            throw new ServiceClientException(
                $"'{filePath}' is not a supported file. Only .tar.gz (MongoDB dump) or .octobak.zip (dump including " +
                "archive data) files are supported.");
        }

        var tusEndpointUrl = BuildTusUploadUri(tenantId);

        var fileInfo = new FileInfo(filePath);
        var metadata = new MetadataCollection
        {
            ["tenantId"] = tenantId,
            ["databaseName"] = databaseName,
            ["fileName"] = fileInfo.Name,
            ["contentType"] = isZip ? MimeTypes.MimeTypeZip : MimeTypes.MimeTypeGzip
        };

        if (!string.IsNullOrWhiteSpace(oldDatabaseName))
        {
            metadata["oldDatabaseName"] = oldDatabaseName!;
        }

        var tusFileId = await UploadWithTusAsync(tusEndpointUrl, metadata, fileInfo, progressCallback,
            cancellationToken);

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

        using var httpClient = ServerCertificateTrust.CreateHttpClient();
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("bearer", AccessToken.AccessToken);
        httpClient.Timeout = TimeSpan.FromHours(2);

        using var response = await httpClient.GetAsync(uriBuilder.Uri, HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var expectedLength = response.Content.Headers.ContentLength;

        // The body goes to a temporary file next to the target and replaces the target only when it is complete,
        // so a broken connection never leaves a truncated file under the real name (AB#6969).
        var temporaryFilePath = outputFilePath + "." + Guid.NewGuid().ToString("N") + ".part";
        long totalBytesRead = 0;
        try
        {
            try
            {
                using (var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken))
                using (var fileStream = new FileStream(temporaryFilePath, FileMode.CreateNew, FileAccess.Write,
                           FileShare.None, bufferSize: 81920, useAsync: true))
                {
                    var buffer = new byte[81920];
                    int bytesRead;

                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                        totalBytesRead += bytesRead;
                        progressCallback?.Invoke(totalBytesRead);
                    }

                    await fileStream.FlushAsync(cancellationToken);
                }
            }
            catch (IOException e)
            {
                throw new IOException(
                    $"The download of job '{jobId}' broke off after {totalBytesRead:N0} bytes: {e.Message}", e);
            }

            if (expectedLength.HasValue && totalBytesRead != expectedLength.Value)
            {
                throw new IOException(
                    $"The download of job '{jobId}' is incomplete: {totalBytesRead:N0} of {expectedLength.Value:N0} bytes were received.");
            }

            File.Move(temporaryFilePath, outputFilePath, overwrite: true);
        }
        catch
        {
            TryDeleteFile(temporaryFilePath);
            throw;
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Nothing more to do; the original failure is the one the caller needs to see.
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
        var tusEndpointUrl = BuildTusUploadUri(tenantId);

        var fileInfo = new FileInfo(filePath);
        var metadata = new MetadataCollection
        {
            ["tenantId"] = tenantId,
            ["archiveRtId"] = archiveRtId,
            ["fileName"] = fileInfo.Name,
            ["contentType"] = MimeTypes.MimeTypeZip
        };

        var tusFileId = await UploadWithTusAsync(tusEndpointUrl, metadata, fileInfo, progressCallback,
            cancellationToken);

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
    public async Task<JobResponseDto> StartSecretSweepAsync(string tenantId,
        SecretSweepModeDto mode = SecretSweepModeDto.Verify, bool confirm = false)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);
        EnsureSweepModeOffered(mode);

        var request = new RestRequest(BuildTenantJobUri(tenantId, "secret-sweep"), Method.Post);
        request.AddQueryParameter("mode", mode.ToString());
        if (confirm)
        {
            request.AddQueryParameter("confirm", "true");
        }

        var response = await Client.ExecuteAsync<JobResponseDto>(request);
        ValidateResponse(response);

        return response.Data ?? throw ServiceClientResultException.NoDataReturned();
    }

    /// <inheritdoc />
    public async Task<SecretSweepReportDto?> GetSecretSweepReportAsync(string tenantId)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);

        var request = new RestRequest(BuildTenantJobUri(tenantId, "secret-sweep/report"));

        var response = await Client.ExecuteAsync<SecretSweepReportDto>(request);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        ValidateResponse(response);

        return response.Data ?? throw ServiceClientResultException.NoDataReturned();
    }

    /// <inheritdoc />
    public async Task<JobResponseDto> StartSecretSweepAllTenantsAsync(
        SecretSweepModeDto mode = SecretSweepModeDto.Verify, bool confirm = false)
    {
        EnsureSweepModeOffered(mode);

        // System API: the sweep spans all tenants and is gated on the system tenant by the service.
        var request = new RestRequest("secrets/sweep", Method.Post);
        request.AddQueryParameter("mode", mode.ToString());
        if (confirm)
        {
            request.AddQueryParameter("confirm", "true");
        }

        var response = await Client.ExecuteAsync<JobResponseDto>(request);
        ValidateResponse(response);

        return response.Data ?? throw ServiceClientResultException.NoDataReturned();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SecretSweepReportDto>> GetSecretSweepReportsAsync()
    {
        var request = new RestRequest("secrets/reports");

        var response = await Client.ExecuteAsync<List<SecretSweepReportDto>>(request);
        ValidateResponse(response);

        return response.Data ?? throw ServiceClientResultException.NoDataReturned();
    }

    /// <inheritdoc />
    public async Task<SecretEnvironmentStatusDto> GetSecretEnvironmentStatusAsync(string tenantId)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);

        var request = new RestRequest(BuildTenantSecretsUri(tenantId, "status"));

        var response = await Client.ExecuteAsync<SecretEnvironmentStatusDto>(request);
        ValidateResponse(response);

        return response.Data ?? throw ServiceClientResultException.NoDataReturned();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SecretSweepRunDto>> GetSecretSweepRunsAsync(string tenantId, int limit = 20)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);

        var request = new RestRequest(BuildTenantSecretsUri(tenantId, "sweep-runs"));
        request.AddQueryParameter("limit", limit.ToString(System.Globalization.CultureInfo.InvariantCulture));

        var response = await Client.ExecuteAsync<List<SecretSweepRunDto>>(request);
        ValidateResponse(response);

        return response.Data ?? throw ServiceClientResultException.NoDataReturned();
    }

    /// <inheritdoc />
    public async Task<SecretSweepDumpDeleteResultDto> DeleteSecretSweepDumpAsync(string tenantId, string runId)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);
        ArgumentValidation.ValidateString(nameof(runId), runId);

        var request = new RestRequest(
            BuildTenantSecretsUri(tenantId, "sweep-runs", Uri.EscapeDataString(runId), "dump"), Method.Delete);

        var response = await Client.ExecuteAsync(request);
        switch (response.StatusCode)
        {
            case HttpStatusCode.NotFound:
                return SecretSweepDumpDeleteResultDto.NotFound;
            case HttpStatusCode.Conflict:
                return SecretSweepDumpDeleteResultDto.AlreadyDeleted;
        }

        ValidateResponse(response);
        return SecretSweepDumpDeleteResultDto.Deleted;
    }

    /// <inheritdoc />
    public async Task<JobResponseDto> RestoreSecretSweepDumpAsync(string tenantId, string runId, bool confirm)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);
        ArgumentValidation.ValidateString(nameof(runId), runId);

        var request = new RestRequest(
            BuildTenantSecretsUri(tenantId, "sweep-runs", Uri.EscapeDataString(runId), "restore-dump"), Method.Post);
        if (confirm)
        {
            request.AddQueryParameter("confirm", "true");
        }

        var response = await Client.ExecuteAsync<JobResponseDto>(request);
        var failure = MapRestoreDumpFailure(response);
        if (failure.HasValue)
        {
            // Fixed message per reason: the body is used only to tell the two 409 codes apart.
            throw new SecretSweepDumpRestoreException(failure.Value, response.StatusCode);
        }

        ValidateResponse(response);

        return response.Data ?? throw ServiceClientResultException.NoDataReturned();
    }

    /// <summary>
    ///     Maps the refusals of the pre-sweep dump restore to <see cref="SecretSweepDumpRestoreFailure" />;
    ///     <c>null</c> for everything else (success, <c>401</c>, <c>403</c>, server errors, unknown <c>400</c>s).
    /// </summary>
    private static SecretSweepDumpRestoreFailure? MapRestoreDumpFailure(RestResponse response)
    {
        return response.StatusCode switch
        {
            HttpStatusCode.NotFound => SecretSweepDumpRestoreFailure.NotFound,
            HttpStatusCode.BadRequest when ReadErrorCode(response) == "ConfirmationRequired" =>
                SecretSweepDumpRestoreFailure.ConfirmationRequired,
            HttpStatusCode.Conflict when ReadErrorCode(response) == SecretEnvironmentWarningCodes.DumpKeyMissing =>
                SecretSweepDumpRestoreFailure.DumpKeyMissing,
            HttpStatusCode.Conflict => SecretSweepDumpRestoreFailure.DumpDeleted,
            _ => null
        };
    }

    /// <summary>
    ///     Reads the machine-readable error code bot services puts into <c>statusDescription</c> of a coded error
    ///     answer (<c>{"statusCode":400,"statusDescription":"ConfirmationRequired","message":"..."}</c>); <c>null</c>
    ///     when the body is empty or not such an object.
    /// </summary>
    private static string? ReadErrorCode(RestResponse response)
    {
        if (string.IsNullOrWhiteSpace(response.Content))
        {
            return null;
        }

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(response.Content);
            if (document.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object)
            {
                return null;
            }

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (string.Equals(property.Name, "statusDescription", StringComparison.OrdinalIgnoreCase) &&
                    property.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    return property.Value.GetString();
                }
            }
        }
        catch (System.Text.Json.JsonException)
        {
            // Not JSON: no code.
        }

        return null;
    }

    /// <summary>
    ///     The service refuses <see cref="SecretSweepModeDto.Decrypt" /> with <c>400</c> (an emergency
    ///     operation that writes clear text back); refusing it here gives the caller a precise error
    ///     without a round trip.
    /// </summary>
    private static void EnsureSweepModeOffered(SecretSweepModeDto mode)
    {
        if (mode is not (SecretSweepModeDto.Verify or SecretSweepModeDto.Encrypt or SecretSweepModeDto.Reprotect
            or SecretSweepModeDto.CleanupUnreadable))
        {
            throw new ArgumentOutOfRangeException(nameof(mode), mode,
                "Secret sweep mode is not available; use Verify, Encrypt, Reprotect or CleanupUnreadable.");
        }
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