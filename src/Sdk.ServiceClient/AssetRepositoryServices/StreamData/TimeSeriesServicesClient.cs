using System.Net.Http.Headers;
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
    public async Task<Stream> ExportArchiveRowsAsync(string tenantId, string archiveRtId, DateTime? fromUtc,
        DateTime? toUtc, CancellationToken ct = default)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);
        ArgumentValidation.ValidateString(nameof(archiveRtId), archiveRtId);

        var uri = ServiceUri.Append("streamdata", "archives", archiveRtId, "export-stream");
        var query = $"tenantId={Uri.EscapeDataString(tenantId)}";
        if (fromUtc.HasValue)
        {
            query += $"&fromUtc={Uri.EscapeDataString(fromUtc.Value.ToUniversalTime().ToString("O"))}";
        }

        if (toUtc.HasValue)
        {
            query += $"&toUtc={Uri.EscapeDataString(toUtc.Value.ToUniversalTime().ToString("O"))}";
        }

        var uriBuilder = new UriBuilder(uri) { Query = query };

        // Use a raw HttpClient so the response body is streamed (RestSharp buffers the whole
        // response). The caller owns the returned stream (and its underlying response/handler);
        // disposing the stream tears the request down.
        var httpClient = new HttpClient { Timeout = TimeSpan.FromHours(2) };
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("bearer", AccessToken.AccessToken);

        HttpResponseMessage? response = null;
        try
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-ndjson"));

            response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            var contentStream = await response.Content.ReadAsStreamAsync();
            return new HttpOwningStream(contentStream, response, httpClient);
        }
        catch
        {
            response?.Dispose();
            httpClient.Dispose();
            throw;
        }
    }

    /// <inheritdoc />
    public async Task ImportArchiveRowsAsync(string tenantId, string archiveRtId, Stream ndjson,
        ArchiveImportMode mode, CancellationToken ct = default)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);
        ArgumentValidation.ValidateString(nameof(archiveRtId), archiveRtId);
        ArgumentValidation.Validate(nameof(ndjson), ndjson);

        var uri = ServiceUri.Append("streamdata", "archives", archiveRtId, "import-stream");
        var uriBuilder = new UriBuilder(uri)
        {
            Query = $"tenantId={Uri.EscapeDataString(tenantId)}&mode={mode}"
        };

        // Stream the body straight through without buffering the whole dataset.
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromHours(2) };
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("bearer", AccessToken.AccessToken);

        using var content = new StreamContent(ndjson);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-ndjson");

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, uriBuilder.Uri) { Content = content };

        using var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new ServiceClientResultException(
                string.IsNullOrEmpty(body) ? $"The call was not successful: {response.StatusCode}" : body,
                response.StatusCode,
                null);
        }
    }

    /// <inheritdoc />
    public async Task<ArchiveSchemaDto> GetArchiveSchemaAsync(string tenantId, string archiveRtId,
        CancellationToken ct = default)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);
        ArgumentValidation.ValidateString(nameof(archiveRtId), archiveRtId);

        var request = new RestRequest($"streamdata/archives/{archiveRtId}/schema", Method.Get);
        request.AddQueryParameter("tenantId", tenantId);

        var response = await Client.ExecuteAsync<ArchiveSchemaDto>(request, ct);
        ValidateResponse(response);

        if (response.Data == null)
        {
            throw ServiceClientResultException.NoDataReturned();
        }

        return response.Data;
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