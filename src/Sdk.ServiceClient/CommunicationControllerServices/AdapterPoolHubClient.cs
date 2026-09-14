using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Communication.Contracts.Hubs;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// ReSharper disable UnusedMember.Global

namespace Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;

/// <summary>
///     Client proxy for the adapter pool management hub of the communication controller
///     (AB#4924, concept §4).
/// </summary>
/// <remarks>
///     Mirrors <see cref="OperatorHubClient" /> rather than <see cref="AdapterHubClient" />, because
///     the thing it has in common with the operator channel is the one that shapes it: the
///     connection is <b>not</b> tenant-addressed. See <see cref="IAdapterPoolHub" /> for why a pool
///     member cannot use <c>/{tenantId}/adapterHub</c>.
/// </remarks>
public class AdapterPoolHubClient : SignalRClient<AdapterPoolHubClientOptions>, IAdapterPoolHubClient
{
    /// <summary>
    ///     The hub path. A constant rather than a literal in two places, because the controller's
    ///     route and this override have to agree and nothing else would notice if they drifted.
    /// </summary>
    public const string HubName = "adapterPoolHub";

    private readonly IAdapterPoolHubCallbacks _callbacks;

    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="options">Options for configuration of the client proxy.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="serviceClientAccessToken">The access token management object.</param>
    /// <param name="callbacks">Handlers for the lease and drain pushes.</param>
    public AdapterPoolHubClient(IOptions<AdapterPoolHubClientOptions> options,
        ILogger<AdapterPoolHubClient> logger, IServiceClientAccessToken serviceClientAccessToken,
        IAdapterPoolHubCallbacks callbacks)
        : this(options.Value, logger, serviceClientAccessToken, callbacks)
    {
    }

    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="options">Options for configuration of the client proxy.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="serviceClientAccessToken">The access token management object.</param>
    /// <param name="callbacks">Handlers for the lease and drain pushes.</param>
    public AdapterPoolHubClient(AdapterPoolHubClientOptions options,
        ILogger<AdapterPoolHubClient> logger, IServiceClientAccessToken serviceClientAccessToken,
        IAdapterPoolHubCallbacks callbacks)
        : base(options, logger, serviceClientAccessToken, HubName)
    {
        _callbacks = callbacks;
    }

    /// <summary>
    ///     Binds the server-to-client callbacks on <b>every</b> (re-)created connection — the same
    ///     rule as the other two hub clients. Binding once in the constructor loses every push after
    ///     a stop/start cycle, and on this channel a lost push is a lease that is granted
    ///     server-side and never acted on.
    /// </summary>
    protected override void RegisterServerCallbacks(HubConnection hubConnection)
    {
        hubConnection.On<LeaseDto>(nameof(IAdapterPoolHubCallbacks.LeaseAsync), _callbacks.LeaseAsync);
        hubConnection.On<string>(nameof(IAdapterPoolHubCallbacks.DrainAsync), _callbacks.DrainAsync);
    }

    /// <summary>
    ///     Builds the tenant-free hub URI. A pool member's management connection addresses no tenant,
    ///     which is exactly why it cannot use the base implementation — that one throws when the
    ///     options carry no tenant, and it is right to, for every tenant-addressed hub.
    /// </summary>
    protected override Uri BuildServiceUri()
    {
        if (string.IsNullOrWhiteSpace(Options.EndpointUri))
        {
            throw new ServiceConfigurationMissingException("Communication Controller service URI is not configured.");
        }

        return new Uri(Options.EndpointUri).Append(HubName);
    }

    /// <inheritdoc />
    public async Task<PoolMemberRegistrationResultDto> RegisterPoolMemberAsync(
        PoolMemberRegistrationDto registration)
    {
        return await HubConnection.InvokeAsync<PoolMemberRegistrationResultDto>(
            nameof(IAdapterPoolHub.RegisterPoolMemberAsync), registration);
    }

    /// <inheritdoc />
    public async Task ReleaseLeaseAsync(LeaseResultDto result)
    {
        await HubConnection.InvokeAsync(nameof(IAdapterPoolHub.ReleaseLeaseAsync), result);
    }

    /// <inheritdoc />
    public async Task HeartbeatAsync(PoolMemberHeartbeatDto heartbeat)
    {
        await HubConnection.InvokeAsync(nameof(IAdapterPoolHub.HeartbeatAsync), heartbeat);
    }
}
