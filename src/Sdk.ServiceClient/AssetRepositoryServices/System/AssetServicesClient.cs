using Meshmakers.Common.Shared;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.System;

/// <summary>
///     Implementation of the client proxy for asset services on system level.
/// </summary>
public class AssetServicesClient : ServiceClient, IAssetServicesClient
{
    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="serviceClientOptions">Options for configuration of the client proxy.</param>
    /// <param name="assetAccessToken">The access token management object</param>
    public AssetServicesClient(IOptions<AssetServiceClientOptions> serviceClientOptions,
        IAssetServiceClientAccessToken assetAccessToken)
        : this(serviceClientOptions.Value, assetAccessToken)
    {
    }

    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="serviceClientOptions">Options for configuration of the client proxy.</param>
    /// <param name="assetAccessToken">The access token management object</param>
    public AssetServicesClient(AssetServiceClientOptions serviceClientOptions,
        IAssetServiceClientAccessToken assetAccessToken)
        : base(serviceClientOptions, assetAccessToken)
    {
    }

    /// <inheritdoc />
    public async Task<JobDto> GetImportJobStatusAsync(string id)
    {
        ArgumentValidation.ValidateString(nameof(id), id);

        var request = new RestRequest("jobs");
        request.AddQueryParameter("id", id);

        var response = await Client.ExecuteAsync<JobDto>(request);
        ValidateResponse(response);

        return response.Data!;
    }

    /// <inheritdoc />
    public async Task<string> ImportCkModelAsync(string tenantId, string ckModelFilePath)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);
        ArgumentValidation.ValidateExistingFile(nameof(ckModelFilePath), ckModelFilePath);

        var request = new RestRequest("models/ImportCk", Method.Post);

        if (Path.GetExtension(ckModelFilePath).ToLower() == ".zip")
        {
            request.AddFile("file", ckModelFilePath, MimeTypes.MimeTypeZip);
        }
        else if (Path.GetExtension(ckModelFilePath).ToLower() == ".json")
        {
            request.AddFile("file", ckModelFilePath, MimeTypes.MimeTypeJson);
        }
        else if (Path.GetExtension(ckModelFilePath).ToLower() == ".yaml")
        {
            request.AddFile("file", ckModelFilePath, MimeTypes.MimeTypeYaml);
        }
        else
        {
            throw new ServiceClientException($"'{ckModelFilePath}' is not a supported file.");
        }

        var response = await Client.ExecuteAsync<TransferModelResponseDto>(request);
        ValidateResponse(response);

        if (response.Data == null)
        {
            throw ServiceClientResultException.NoDataReturned();
        }

        return response.Data.JobId;
    }

    /// <inheritdoc />
    public async Task<string> ImportRtModelAsync(string tenantId, ImportStrategyDto importStrategy, string rtModelFilePath)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);
        ArgumentValidation.ValidateExistingFile(nameof(rtModelFilePath), rtModelFilePath);

        var request = new RestRequest("models/ImportRt", Method.Post);
        request.AddQueryParameter("importStrategy", importStrategy);

        if (Path.GetExtension(rtModelFilePath).ToLower() == ".zip")
        {
            request.AddFile("file", rtModelFilePath, MimeTypes.MimeTypeZip);
        }
        else if (Path.GetExtension(rtModelFilePath).ToLower() == ".yaml")
        {
            request.AddFile("file", rtModelFilePath, MimeTypes.MimeTypeYaml);
        }
        else if (Path.GetExtension(rtModelFilePath).ToLower() == ".json")
        {
            request.AddFile("file", rtModelFilePath, MimeTypes.MimeTypeJson);
        }
        else
        {
            throw new ServiceClientException($"'{rtModelFilePath}' is not a supported file.");
        }

        var response = await Client.ExecuteAsync<TransferModelResponseDto>(request);
        ValidateResponse(response);
        
        if (response.Data == null)
        {
            throw ServiceClientResultException.NoDataReturned();
        }

        return response.Data.JobId;
    }

    /// <inheritdoc />
    public async Task<string> ExportRtModelByQueryAsync(string tenantId, OctoObjectId queryId)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);

        var request = new RestRequest("models/ExportRtByQuery", Method.Post);
        request.AddJsonBody(new ExportModelRequestByQueryDto { QueryId = queryId });

        var response = await Client.ExecuteAsync<TransferModelResponseDto>(request);
        ValidateResponse(response);
        
        if (response.Data == null)
        {
            throw ServiceClientResultException.NoDataReturned();
        }

        return response.Data.JobId;
    }
    
    /// <inheritdoc />
    public async Task<string> ExportRtModelByDeepGraphAsync(string tenantId, IEnumerable<OctoObjectId> originRtIds,
        RtCkId<CkTypeId> originCkTypeId)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);

        var request = new RestRequest("models/ExportRtByDeepGraph", Method.Post);
        request.AddJsonBody(new ExportModelRequestByDeepGraphDto(originCkTypeId, originRtIds));

        var response = await Client.ExecuteAsync<TransferModelResponseDto>(request);
        ValidateResponse(response);

        if (response.Data == null)
        {
            throw ServiceClientResultException.NoDataReturned();
        }

        return response.Data.JobId;
    }

    /// <inheritdoc />
    public async Task CleanTenantAsync(string childTenantId)
    {
        ArgumentValidation.ValidateString(nameof(childTenantId), childTenantId);

        var request = new RestRequest("tenants/clear", Method.Put);
        request.AddQueryParameter("childTenantId", childTenantId);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task UpdateSystemCkModelOfTenant(string childTenantId)
    {
        ArgumentValidation.ValidateString(nameof(childTenantId), childTenantId);

        var request = new RestRequest("tenants/update", Method.Put);
        request.AddQueryParameter("childTenantId", childTenantId);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task ClearTenantCacheAsync(string childTenantId)
    {
        var request = new RestRequest("tenants/clearCache", Method.Put);
        request.AddQueryParameter("childTenantId", childTenantId);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TenantDto>> GetTenantsAsync()
    {
        var request = new RestRequest("tenants");

        var response = await Client.ExecuteAsync<List<TenantDto>>(request);
        ValidateResponse(response);

        return response.Data ?? new List<TenantDto>();
    }

    /// <inheritdoc />
    public async Task CreateTenantAsync(string childTenantId, string databaseName)
    {
        ArgumentValidation.ValidateString(nameof(childTenantId), childTenantId);
        ArgumentValidation.ValidateString(nameof(databaseName), databaseName);

        var request = new RestRequest("tenants", Method.Post);
        request.AddQueryParameter("childTenantId", childTenantId);
        request.AddQueryParameter("databaseName", databaseName);

        var response = await Client.ExecutePostAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task AttachTenantAsync(string childTenantId, string databaseName)
    {
        ArgumentValidation.ValidateString(nameof(childTenantId), childTenantId);
        ArgumentValidation.ValidateString(nameof(databaseName), databaseName);

        var request = new RestRequest("tenants/attach", Method.Post);
        request.AddQueryParameter("childTenantId", childTenantId);
        request.AddQueryParameter("databaseName", databaseName);

        var response = await Client.ExecutePostAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task DetachTenantAsync(string childTenantId)
    {
        ArgumentValidation.ValidateString(nameof(childTenantId), childTenantId);

        var request = new RestRequest("tenants/detach", Method.Post);
        request.AddQueryParameter("childTenantId", childTenantId);

        var response = await Client.ExecutePostAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task DeleteTenantAsync(string childTenantId)
    {
        ArgumentValidation.ValidateString(nameof(childTenantId), childTenantId);

        var request = new RestRequest("tenants", Method.Delete);
        request.AddQueryParameter("childTenantId", childTenantId);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
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
            throw new ServiceConfigurationMissingException("Asset Repo Services URI is missing.");
        }

        var assetOptions = (AssetServiceClientOptions)Options;
        if (string.IsNullOrWhiteSpace(assetOptions.TenantId))
        {
            throw new ServiceConfigurationMissingException("Asset Repo Services tenant ID is missing.");
        }

        return new Uri(Options.EndpointUri).Append(assetOptions.TenantId!).Append("v1");
    }
}