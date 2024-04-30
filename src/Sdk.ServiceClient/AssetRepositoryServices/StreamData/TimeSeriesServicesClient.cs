using Meshmakers.Common.Shared;
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
    protected override Uri BuildServiceUri()
    {
        if (string.IsNullOrWhiteSpace(Options.EndpointUri))
        {
            throw new ServiceConfigurationMissingException("StreamData services URI is missing");
        }

        return new Uri($"{Options.EndpointUri!.EnsureEndsWith("/")}api/v1");
    }
}