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
    public async Task<JobResponseDto> RestoreRepositoryAsync(string tenantId, string databaseName, string filePath, string? oldDatabaseName = null)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);
        ArgumentValidation.ValidateString(nameof(databaseName), databaseName);
        ArgumentValidation.ValidateExistingFile(nameof(filePath), filePath);

        var request = new RestRequest("jobs/restore-repository", Method.Post);
        request.AddQueryParameter("tenantId", tenantId);
        request.AddQueryParameter("databaseName", databaseName);
        if (!string.IsNullOrWhiteSpace(oldDatabaseName))
        {
            request.AddQueryParameter("oldDatabaseName", oldDatabaseName);
        }

        if (Path.GetExtension(filePath).ToLower() == ".gz")
        {
            request.AddFile("file", filePath, MimeTypes.MimeTypeGzip);
        }
        else
        {
            throw new ServiceClientException($"'{filePath}' is not a supported file.");
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
    public async Task<JobResponseDto> CompareLiveTenantsAsync(string tenantId, string sourceTenantId,
        string targetTenantId, string? optionsJson = null)
    {
        ArgumentValidation.ValidateString(nameof(sourceTenantId), sourceTenantId);
        ArgumentValidation.ValidateString(nameof(targetTenantId), targetTenantId);
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);

        var request = new RestRequest("jobs/compare-live-tenants", Method.Post);
        request.AddQueryParameter("tenantId", tenantId);

        // Build the request body using the proper request model
        TenantComparisonOptionsDto? options = null;
        if (!string.IsNullOrWhiteSpace(optionsJson))
        {
            options = System.Text.Json.JsonSerializer.Deserialize<TenantComparisonOptionsDto>(optionsJson!);
        }

        var requestBody = new CompareLiveTenantsRequest
        {
            SourceTenantId = sourceTenantId,
            TargetTenantId = targetTenantId,
            Options = options
        };

        request.AddJsonBody(requestBody);

        var response = await Client.ExecuteAsync<JobResponseDto>(request);
        ValidateResponse(response);

        if (response.Data == null)
        {
            throw ServiceClientResultException.NoDataReturned();
        }

        return response.Data;
    }

    /// <inheritdoc />
    public async Task<JobResponseDto> CompareLiveTenantWithBackupAsync(string tenantId, string sourceTenantId,
        string backupFilePath, string? optionsJson = null)
    {
        ArgumentValidation.ValidateString(nameof(sourceTenantId), sourceTenantId);
        ArgumentValidation.ValidateExistingFile(nameof(backupFilePath), backupFilePath);
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);

        var request = new RestRequest("jobs/compare-tenant-with-backup", Method.Post);
        request.AddQueryParameter("tenantId", tenantId);

        // Add form parameters matching the request model structure
        request.AddParameter("tenantId", sourceTenantId);
        request.AddParameter("systemTenantId", tenantId);

        // Parse and add options if provided
        if (!string.IsNullOrWhiteSpace(optionsJson))
        {
            var options = System.Text.Json.JsonSerializer.Deserialize<TenantComparisonOptionsDto>(optionsJson!);
            if (options != null)
            {
                request.AddParameter("options", System.Text.Json.JsonSerializer.Serialize(options));
            }
        }

        if (Path.GetExtension(backupFilePath).ToLower() == ".gz")
        {
            request.AddFile("backupFile", backupFilePath, MimeTypes.MimeTypeGzip);
        }
        else
        {
            throw new ServiceClientException($"'{backupFilePath}' is not a supported file. Expected .gz extension.");
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
    public async Task<JobResponseDto> CompareBackupsAsync(string tenantId, string sourceBackupFilePath,
        string targetBackupFilePath, string? optionsJson = null)
    {
        ArgumentValidation.ValidateExistingFile(nameof(sourceBackupFilePath), sourceBackupFilePath);
        ArgumentValidation.ValidateExistingFile(nameof(targetBackupFilePath), targetBackupFilePath);
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);

        var request = new RestRequest("jobs/compare-backups", Method.Post);
        request.AddQueryParameter("tenantId", tenantId);

        // Add form parameters matching the request model structure
        request.AddParameter("systemTenantId", tenantId);

        // Parse and add options if provided
        if (!string.IsNullOrWhiteSpace(optionsJson))
        {
            var options = System.Text.Json.JsonSerializer.Deserialize<TenantComparisonOptionsDto>(optionsJson!);
            if (options != null)
            {
                request.AddParameter("options", System.Text.Json.JsonSerializer.Serialize(options));
            }
        }

        if (Path.GetExtension(sourceBackupFilePath).ToLower() == ".gz")
        {
            request.AddFile("sourceBackupFile", sourceBackupFilePath, MimeTypes.MimeTypeGzip);
        }
        else
        {
            throw new ServiceClientException($"'{sourceBackupFilePath}' is not a supported file. Expected .gz extension.");
        }

        if (Path.GetExtension(targetBackupFilePath).ToLower() == ".gz")
        {
            request.AddFile("targetBackupFile", targetBackupFilePath, MimeTypes.MimeTypeGzip);
        }
        else
        {
            throw new ServiceClientException($"'{targetBackupFilePath}' is not a supported file. Expected .gz extension.");
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
    protected override Uri BuildServiceUri()
    {
        if (string.IsNullOrWhiteSpace(Options.EndpointUri))
        {
            throw new ServiceConfigurationMissingException("Bot services URI is missing");
        }

        return new Uri(Options.EndpointUri).Append("system").Append("v1");
    }
}