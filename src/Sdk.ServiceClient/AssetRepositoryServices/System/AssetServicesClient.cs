using Meshmakers.Common.Shared;
using Meshmakers.Octo.Common.Shared;
using Meshmakers.Octo.Common.Shared.DataTransferObjects;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.System;

/// <summary>
/// Implementation of the client proxy for asset services on system level.
/// </summary>
public class AssetServicesClient : ServiceClient, IAssetServicesClient
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="serviceClientOptions">Options for configuration of the client proxy.</param>
    /// <param name="assetAccessToken">The access token management object</param>
    public AssetServicesClient(IOptions<AssetServiceClientOptions> serviceClientOptions,
        IAssetServiceClientAccessToken assetAccessToken)
        : this(serviceClientOptions.Value, assetAccessToken)
    {
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="serviceClientOptions">Options for configuration of the client proxy.</param>
    /// <param name="assetAccessToken">The access token management object</param>
    public AssetServicesClient(AssetServiceClientOptions serviceClientOptions,
        IAssetServiceClientAccessToken assetAccessToken)
        : base(serviceClientOptions, assetAccessToken)
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
    public async Task<string> ImportCkModel(string tenantId, ScopeIdsDto scopeId, string ckModelFilePath)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);
        ArgumentValidation.ValidateExistingFile(nameof(ckModelFilePath), ckModelFilePath);

        var request = new RestRequest("models/ImportCk", Method.Post);
        request.AddQueryParameter("tenantId", tenantId);
        request.AddQueryParameter("scopeId", ((int)scopeId).ToString());

        if (Path.GetExtension(ckModelFilePath).ToLower() == ".zip")
        {
            request.AddFile("file", ckModelFilePath, "application/zip");
        }
        else if (Path.GetExtension(ckModelFilePath).ToLower() == ".json")
        {
            request.AddFile("file", ckModelFilePath, "application/json");
        }
        else
        {
            throw new ServiceClientException($"'{ckModelFilePath}' is not a supported file.");
        }

        var response = await Client.ExecuteAsync<string>(request);
        ValidateResponse(response);

        return response.Data!;
    }

    /// <inheritdoc />
    public async Task<string> ImportRtModel(string tenantId, string rtModelFilePath)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);
        ArgumentValidation.ValidateExistingFile(nameof(rtModelFilePath), rtModelFilePath);

        var request = new RestRequest("models/ImportRt", Method.Post);
        request.AddQueryParameter("tenantId", tenantId);

        if (Path.GetExtension(rtModelFilePath).ToLower() == ".zip")
        {
            request.AddFile("file", rtModelFilePath, "application/zip");
        }
        else if (Path.GetExtension(rtModelFilePath).ToLower() == ".json")
        {
            request.AddFile("file", rtModelFilePath, "application/json");
        }
        else
        {
            throw new ServiceClientException($"'{rtModelFilePath}' is not a supported file.");
        }

        var response = await Client.ExecuteAsync<string>(request);
        ValidateResponse(response);

        return response.Data!;
    }

    /// <inheritdoc />
    public async Task<string> ExportRtModel(string tenantId, OctoObjectId queryId)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);

        var request = new RestRequest("models/ExportRt", Method.Post);
        request.AddQueryParameter("tenantId", tenantId);
        request.AddJsonBody(new ExportModelRequestDto { QueryId = queryId });

        var response = await Client.ExecuteAsync<string>(request);
        ValidateResponse(response);

        return response.Data!;
    }

    /// <inheritdoc />
    public async Task CleanTenant(string tenantId)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);

        var request = new RestRequest("tenants/clear", Method.Put);
        request.AddQueryParameter("tenantId", tenantId);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task UpdateSystemCkModelOfTenant(string tenantId)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);

        var request = new RestRequest("tenants/update", Method.Put);
        request.AddQueryParameter("tenantId", tenantId);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task ClearTenantCache(string tenantId)
    {
        var request = new RestRequest("tenants/clearCache", Method.Put);
        request.AddQueryParameter("tenantId", tenantId);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TenantDto>> GetTenants()
    {
        var request = new RestRequest("tenants");

        var response = await Client.ExecuteAsync<List<TenantDto>>(request);
        ValidateResponse(response);

        return response.Data ?? new List<TenantDto>();
    }

    /// <inheritdoc />
    public async Task CreateTenant(string tenantId, string databaseName)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);
        ArgumentValidation.ValidateString(nameof(databaseName), databaseName);

        var request = new RestRequest("tenants", Method.Post);
        request.AddQueryParameter("tenantId", tenantId);
        request.AddQueryParameter("databaseName", databaseName);

        var response = await Client.ExecutePostAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task AttachTenant(string tenantId, string databaseName)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);
        ArgumentValidation.ValidateString(nameof(databaseName), databaseName);

        var request = new RestRequest("tenants/attach", Method.Post);
        request.AddQueryParameter("tenantId", tenantId);
        request.AddQueryParameter("databaseName", databaseName);

        var response = await Client.ExecutePostAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task DetachTenant(string tenantId)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);

        var request = new RestRequest("dataSources/detach", Method.Post);
        request.AddQueryParameter("tenantId", tenantId);

        var response = await Client.ExecutePostAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task DeleteTenant(string tenantId)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);

        var request = new RestRequest("tenants", Method.Delete);
        request.AddQueryParameter("tenantId", tenantId);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    protected override Uri BuildServiceUri()
    {
        if (string.IsNullOrWhiteSpace(Options.EndpointUri))
        {
            throw new ServiceConfigurationMissingException("Core services URI is missing.");
        }

        return new Uri(Options.EndpointUri).Append("system").Append("v1");
    }
}
