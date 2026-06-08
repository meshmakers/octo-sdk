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

    /// <inheritdoc />
    public async Task<CredentialsStatusDto> RedeemTicketAsync(
        string tenantId,
        string code,
        string accessToken,
        string refreshToken,
        DateTime accessExpiresAt,
        DateTime refreshExpiresAt)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);
        ArgumentValidation.ValidateString(nameof(code), code);
        ArgumentValidation.ValidateString(nameof(accessToken), accessToken);
        ArgumentValidation.ValidateString(nameof(refreshToken), refreshToken);

        // Redeem lives at "/v1/credentials/tickets/redeem" on the service root with
        // [AllowAnonymous]. The configured `Client` would prepend the tenant segment
        // and attach the bearer header — both wrong here. Build a dedicated RestClient
        // anchored at the service root and post against it. Disposed at end of scope
        // so we don't leak handles in CLI scenarios where this runs once.
        if (string.IsNullOrWhiteSpace(Options.EndpointUri))
        {
            throw new ServiceConfigurationMissingException("AI services URI is missing");
        }

        var rootUri = new Uri(Options.EndpointUri).Append("v1");
        using var anonymousClient = new RestClient(rootUri);
        var request = new RestRequest("credentials/tickets/redeem", Method.Post);
        request.AddJsonBody(new
        {
            tenantId,
            code,
            accessToken,
            refreshToken,
            accessExpiresAt,
            refreshExpiresAt,
        });

        var response = await anonymousClient.ExecuteAsync<CredentialsStatusDto>(request);
        ValidateResponse(response);
        return response.Data
               ?? throw new ServiceClientResultException(
                   "Empty body from redeem endpoint", response.StatusCode, response.ErrorException);
    }

    /// <inheritdoc />
    public async Task<CredentialsStatusDto> GetCredentialsStatusAsync()
    {
        if (!IsTenantScoped)
        {
            throw new ServiceConfigurationMissingException(
                "Tenant id is required to read credentials status");
        }

        var request = new RestRequest("credentials/status", Method.Get);
        var response = await Client.ExecuteAsync<CredentialsStatusDto>(request);
        ValidateResponse(response);
        return response.Data
               ?? throw new ServiceClientResultException(
                   "Empty body from status endpoint", response.StatusCode, response.ErrorException);
    }

    /// <inheritdoc />
    public async Task<CredentialsStatusDto> RevokeCredentialsAsync()
    {
        if (!IsTenantScoped)
        {
            throw new ServiceConfigurationMissingException(
                "Tenant id is required to revoke credentials");
        }

        var request = new RestRequest("credentials/revoke", Method.Post);
        var response = await Client.ExecuteAsync<CredentialsStatusDto>(request);
        ValidateResponse(response);
        return response.Data
               ?? throw new ServiceClientResultException(
                   "Empty body from revoke endpoint", response.StatusCode, response.ErrorException);
    }
}
