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

        var request = new RestRequest("jobs/run-fixup-scripts");
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
    public async Task<JobResponseDto> RestoreRepositoryAsync(string tenantId, string databaseName, string filePath)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);
        ArgumentValidation.ValidateString(nameof(databaseName), databaseName);
        ArgumentValidation.ValidateExistingFile(nameof(filePath), filePath);

        var request = new RestRequest("jobs/restore-repository", Method.Post);
        request.AddQueryParameter("tenantId", tenantId);
        request.AddQueryParameter("databaseName", databaseName);

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
    protected override Uri BuildServiceUri()
    {
        if (string.IsNullOrWhiteSpace(Options.EndpointUri))
        {
            throw new ServiceConfigurationMissingException("Bot services URI is missing");
        }

        return new Uri(Options.EndpointUri).Append("system").Append("v1");
    }
}