using Meshmakers.Common.Shared;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.StreamData;

/// <summary>
///     Implementation of the StreamData services client.
/// </summary>
public class StreamDataServicesClient : ServiceClient, IStreamDataServicesClient
{
    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="serviceClientOptions">Options for configuration of the client proxy.</param>
    /// <param name="streamDataAccessToken">The access token management object</param>
    public StreamDataServicesClient(IOptions<StreamDataServiceClientOptions> serviceClientOptions,
        IStreamDataServiceClientAccessToken streamDataAccessToken)
        : this(serviceClientOptions.Value, streamDataAccessToken)
    {
    }

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="serviceClientOptions">Options for configuration of the client proxy.</param>
    /// <param name="streamDataAccessToken">The access token management object</param>
    public StreamDataServicesClient(StreamDataServiceClientOptions serviceClientOptions,
        IStreamDataServiceClientAccessToken streamDataAccessToken)
        : base(serviceClientOptions, streamDataAccessToken)
    {
    }

    /// <inheritdoc />
    public async Task EnableAsync(string tenantId)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);
        
        var request = new RestRequest("streamdata/enable", Method.Post);
        request.AddQueryParameter("tenantId", tenantId);
        
        

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task DisableAsync(string tenantId)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);

        var request = new RestRequest($"streamdata/disable", Method.Post);
        request.AddQueryParameter("tenantId", tenantId);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public Task ActivateArchiveAsync(string tenantId, string archiveRtId)
        => InvokeArchiveTransitionAsync(tenantId, archiveRtId, "activate", Method.Post);

    /// <inheritdoc />
    public Task DisableArchiveAsync(string tenantId, string archiveRtId)
        => InvokeArchiveTransitionAsync(tenantId, archiveRtId, "disable", Method.Post);

    /// <inheritdoc />
    public Task EnableArchiveAsync(string tenantId, string archiveRtId)
        => InvokeArchiveTransitionAsync(tenantId, archiveRtId, "enable", Method.Post);

    /// <inheritdoc />
    public Task RetryArchiveActivationAsync(string tenantId, string archiveRtId)
        => InvokeArchiveTransitionAsync(tenantId, archiveRtId, "retry", Method.Post);

    /// <inheritdoc />
    public Task DeleteArchiveAsync(string tenantId, string archiveRtId)
        => InvokeArchiveTransitionAsync(tenantId, archiveRtId, transitionPath: null, Method.Delete);

    /// <inheritdoc />
    public async Task FreezeRollupArchiveAsync(string tenantId, string rollupRtId, DateTime until)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);
        ArgumentValidation.ValidateString(nameof(rollupRtId), rollupRtId);

        var request = new RestRequest($"streamdata/archives/{rollupRtId}/freeze", Method.Post);
        request.AddQueryParameter("tenantId", tenantId);
        request.AddQueryParameter("until", until.ToUniversalTime().ToString("O"));

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task UnfreezeRollupArchiveAsync(string tenantId, string rollupRtId, bool acceptGaps = false)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);
        ArgumentValidation.ValidateString(nameof(rollupRtId), rollupRtId);

        var request = new RestRequest($"streamdata/archives/{rollupRtId}/unfreeze", Method.Post);
        request.AddQueryParameter("tenantId", tenantId);
        request.AddQueryParameter("acceptGaps", acceptGaps.ToString().ToLowerInvariant());

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task RewindRollupWatermarkAsync(string tenantId, string rollupRtId, DateTime toBucketEnd)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);
        ArgumentValidation.ValidateString(nameof(rollupRtId), rollupRtId);

        var request = new RestRequest($"streamdata/archives/{rollupRtId}/rewind", Method.Post);
        request.AddQueryParameter("tenantId", tenantId);
        request.AddQueryParameter("toBucketEnd", toBucketEnd.ToUniversalTime().ToString("O"));

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RollupArchiveInfoDto>> ListRollupsForArchiveAsync(string tenantId, string archiveRtId)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);
        ArgumentValidation.ValidateString(nameof(archiveRtId), archiveRtId);

        var request = new RestRequest($"streamdata/archives/{archiveRtId}/rollups", Method.Get);
        request.AddQueryParameter("tenantId", tenantId);

        var response = await Client.ExecuteAsync<List<RollupArchiveInfoDto>>(request);
        ValidateResponse(response);
        return response.Data ?? new List<RollupArchiveInfoDto>();
    }

    /// <inheritdoc />
    public async Task<RollupRecomputeJobInfoDto> RecomputeArchiveAsync(
        string tenantId, string rollupRtId, DateTime from, DateTime to, string? rtIdScope = null)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);
        ArgumentValidation.ValidateString(nameof(rollupRtId), rollupRtId);

        var request = new RestRequest($"streamdata/archives/{rollupRtId}/recompute", Method.Post);
        request.AddQueryParameter("tenantId", tenantId);
        request.AddQueryParameter("from", from.ToUniversalTime().ToString("O"));
        request.AddQueryParameter("to", to.ToUniversalTime().ToString("O"));
        if (rtIdScope is not null)
        {
            request.AddQueryParameter("rtIdScope", rtIdScope);
        }

        var response = await Client.ExecuteAsync<RollupRecomputeJobInfoDto>(request);
        ValidateResponse(response);
        return response.Data ?? throw new InvalidOperationException("Recompute response payload was null.");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RollupRecomputeJobInfoDto>> ListRecomputeJobsForArchiveAsync(
        string tenantId, string archiveRtId)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);
        ArgumentValidation.ValidateString(nameof(archiveRtId), archiveRtId);

        var request = new RestRequest($"streamdata/archives/{archiveRtId}/recompute-jobs", Method.Get);
        request.AddQueryParameter("tenantId", tenantId);

        var response = await Client.ExecuteAsync<List<RollupRecomputeJobInfoDto>>(request);
        ValidateResponse(response);
        return response.Data ?? new List<RollupRecomputeJobInfoDto>();
    }

    /// <inheritdoc />
    public async Task AddComputedColumnAsync(
        string tenantId, string archiveRtId, string name, string formula, string resultType, bool indexed = true)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);
        ArgumentValidation.ValidateString(nameof(archiveRtId), archiveRtId);
        ArgumentValidation.ValidateString(nameof(name), name);
        ArgumentValidation.ValidateString(nameof(formula), formula);
        ArgumentValidation.ValidateString(nameof(resultType), resultType);

        var request = new RestRequest($"streamdata/archives/{archiveRtId}/computed-columns", Method.Post);
        request.AddQueryParameter("tenantId", tenantId);
        request.AddQueryParameter("name", name);
        request.AddQueryParameter("formula", formula);
        request.AddQueryParameter("resultType", resultType);
        request.AddQueryParameter("indexed", indexed.ToString().ToLowerInvariant());

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task RemoveComputedColumnAsync(string tenantId, string archiveRtId, string name)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);
        ArgumentValidation.ValidateString(nameof(archiveRtId), archiveRtId);
        ArgumentValidation.ValidateString(nameof(name), name);

        var request = new RestRequest($"streamdata/archives/{archiveRtId}/computed-columns/{name}", Method.Delete);
        request.AddQueryParameter("tenantId", tenantId);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    private async Task InvokeArchiveTransitionAsync(string tenantId, string archiveRtId,
        string? transitionPath, Method method)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);
        ArgumentValidation.ValidateString(nameof(archiveRtId), archiveRtId);

        var resource = transitionPath is null
            ? $"streamdata/archives/{archiveRtId}"
            : $"streamdata/archives/{archiveRtId}/{transitionPath}";

        var request = new RestRequest(resource, method);
        request.AddQueryParameter("tenantId", tenantId);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }


    /// <inheritdoc />
    protected override Uri BuildServiceUri()
    {
        if (string.IsNullOrWhiteSpace(Options.EndpointUri))
        {
            throw new ServiceConfigurationMissingException("StreamData services URI is missing");
        }

        return new Uri($"{Options.EndpointUri!.EnsureEndsWith("/")}api/v1");
    }
}