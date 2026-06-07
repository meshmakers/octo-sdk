using Meshmakers.Common.Shared;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Meshmakers.Octo.Sdk.ServiceClient.AiServices;

/// <summary>
///     REST client for the OctoMesh AI Adapter. Phase 1 covers the System-API enable / disable
///     lifecycle; session and quota operations live behind the SignalR hub and the tenant-scoped
///     REST API, which are not yet projected to the SDK client.
/// </summary>
public class AiServicesClient : ServiceClient, IAiServicesClient
{
    /// <summary>Constructor.</summary>
    /// <param name="serviceClientOptions">Options for configuration of the client proxy.</param>
    /// <param name="accessToken">The access token management object.</param>
    public AiServicesClient(IOptions<AiServiceClientOptions> serviceClientOptions,
        IAiServiceClientAccessToken accessToken)
        : this(serviceClientOptions.Value, accessToken)
    {
    }

    /// <summary>Constructor.</summary>
    /// <param name="serviceClientOptions">Options for configuration of the client proxy.</param>
    /// <param name="accessToken">The access token management object.</param>
    public AiServicesClient(AiServiceClientOptions serviceClientOptions,
        IAiServiceClientAccessToken accessToken)
        : base(serviceClientOptions, accessToken)
    {
    }

    /// <inheritdoc />
    protected override Uri BuildServiceUri()
    {
        if (string.IsNullOrWhiteSpace(Options.EndpointUri))
        {
            throw new ServiceConfigurationMissingException("AI services URI is missing");
        }

        var aiOptions = (AiServiceClientOptions)Options;
        var tenantSegment = !string.IsNullOrWhiteSpace(aiOptions.TenantId)
            ? aiOptions.TenantId!
            : "system";

        return new Uri(Options.EndpointUri).Append(tenantSegment).Append("v1");
    }

    private bool IsTenantScoped =>
        !string.IsNullOrWhiteSpace(((AiServiceClientOptions)Options).TenantId);

    /// <inheritdoc />
    public async Task EnableAsync(string tenantId)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);

        var request = new RestRequest("aiservice/enable", Method.Post);
        if (!IsTenantScoped)
        {
            request.AddQueryParameter("tenantId", tenantId);
        }

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task DisableAsync(string tenantId)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);

        var request = new RestRequest("aiservice/disable", Method.Post);
        if (!IsTenantScoped)
        {
            request.AddQueryParameter("tenantId", tenantId);
        }

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }
}
