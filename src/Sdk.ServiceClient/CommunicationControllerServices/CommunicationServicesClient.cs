using Meshmakers.Common.Shared;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;

/// <summary>
///     Implementation of the client proxy for bot services.
/// </summary>
public class CommunicationServicesClient : ServiceClient, ICommunicationServicesClient
{
    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="serviceClientOptions">Options for configuration of the client proxy.</param>
    /// <param name="accessToken">The access token management object</param>
    public CommunicationServicesClient(IOptions<CommunicationServiceClientOptions> serviceClientOptions,
        ICommunicationServiceClientAccessToken accessToken)
        : this(serviceClientOptions.Value, accessToken)
    {
    }

    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="serviceClientOptions">Options for configuration of the client proxy.</param>
    /// <param name="accessToken">The access token management object</param>
    public CommunicationServicesClient(CommunicationServiceClientOptions serviceClientOptions,
        ICommunicationServiceClientAccessToken accessToken)
        : base(serviceClientOptions, accessToken)
    {
    }

    /// <inheritdoc />
    protected override Uri BuildServiceUri()
    {
        if (string.IsNullOrWhiteSpace(Options.EndpointUri))
        {
            throw new ServiceConfigurationMissingException("Communication services URI is missing");
        }

        return new Uri(Options.EndpointUri).Append("system").Append("v1");
    }

    /// <inheritdoc />
    public async Task EnableAsync(string tenantId)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);

        var request = new RestRequest("communication/enable", Method.Post);
        request.AddQueryParameter("tenantId", tenantId);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task DisableAsync(string tenantId)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);

        var request = new RestRequest("communication/disable", Method.Post);
        request.AddQueryParameter("tenantId", tenantId);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }
}