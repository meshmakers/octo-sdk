using Meshmakers.Octo.Communication.Contracts.Hubs;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;

/// <summary>
///     Implementation of the client proxy for pool hub of communication controller services.
/// </summary>
public class PoolHubClient : SignalRClient<PoolHubClientOptions>, IPoolHubClient
{
    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="serviceClientOptions">Options for configuration of the client proxy.</param>
    /// <param name="logger">Instance of the logger</param>
    /// <param name="serviceClientAccessToken">The access token management object</param>
    /// <param name="poolHubCallbacks">Callbacks for signalr communication</param>
    public PoolHubClient(IOptions<PoolHubClientOptions> serviceClientOptions, ILogger<PoolHubClient> logger,
        IServiceClientAccessToken serviceClientAccessToken, IPoolHubCallbacks poolHubCallbacks)
        : this(serviceClientOptions.Value, logger, serviceClientAccessToken, poolHubCallbacks)
    {
    }

    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="serviceClientOptions">Options for configuration of the client proxy.</param>
    /// <param name="logger">Instance of the logger</param>
    /// <param name="serviceClientAccessToken">The access token management object</param>
    /// <param name="poolHubCallbacks">Callbacks for signalr communication</param>
    public PoolHubClient(PoolHubClientOptions serviceClientOptions, ILogger<PoolHubClient> logger,
        IServiceClientAccessToken serviceClientAccessToken, IPoolHubCallbacks poolHubCallbacks)
        : base(serviceClientOptions, logger, serviceClientAccessToken, "poolHub")
    {
        HubConnection.On<string>(nameof(IPoolHubCallbacks.PreUpdateTenantAsync), poolHubCallbacks.PreUpdateTenantAsync);
    }

    /// <inheritdoc />
    public async Task RegisterPoolOperatorAsync(string poolName)
    {
        await HubConnection.InvokeAsync(nameof(IPoolHub.RegisterPoolOperatorAsync), poolName);
    }

    /// <inheritdoc />
    public async Task UnregisterPoolOperatorAsync(string poolName)
    {
        await HubConnection.InvokeAsync(nameof(IPoolHub.UnregisterPoolOperatorAsync), poolName);
    }

    /// <inheritdoc />
    public async Task UpdateAdapterDeploymentStateAsync(string poolName, RtEntityId adapterRtEntityId, bool deployed)
    {
        await HubConnection.InvokeAsync(nameof(IPoolHub.UpdateAdapterDeploymentStateAsync), poolName, adapterRtEntityId,
            deployed);
    }
}
