using System.Threading.Tasks;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Communication.Contracts.Hubs;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;

namespace Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;

public class PoolHubClient : SignalRClient<PoolHubClientOptions>, IPoolHubClient
{
    public PoolHubClient(IOptions<PoolHubClientOptions> poolControllerServiceClientOptions,
        IServiceClientAccessToken serviceClientAccessToken, IPoolHubCallbacks poolHubCallbacks)
        : this(poolControllerServiceClientOptions.Value, serviceClientAccessToken, poolHubCallbacks)
    {
    }

    public PoolHubClient(PoolHubClientOptions poolHubServiceClientOptions,
        IServiceClientAccessToken serviceClientAccessToken, IPoolHubCallbacks poolHubCallbacks)
        : base(poolHubServiceClientOptions, serviceClientAccessToken, "poolHub")
    {
        HubConnection.On<string, PoolCommunicationAdapterDto>(nameof(IPoolHubCallbacks.DeployCommunicationAdapterAsync),
            poolHubCallbacks.DeployCommunicationAdapterAsync);
        HubConnection.On<string, PoolCommunicationAdapterDto>(nameof(IPoolHubCallbacks.UndeployCommunicationAdapterAsync),
            poolHubCallbacks.UndeployCommunicationAdapterAsync);
    }

    public bool IsAlive => HubConnection.State != HubConnectionState.Disconnected;

    public async Task<PoolConfigurationDto> RegisterPoolOperatorAsync(string poolName)
    {
        return await HubConnection.InvokeAsync<PoolConfigurationDto>(nameof(IPoolHub.RegisterPoolOperatorAsync), poolName);
    }

    public async Task UnregisterPoolOperatorAsync(string poolName)
    {
        await HubConnection.InvokeAsync(nameof(IPoolHub.UnregisterPoolOperatorAsync), poolName);
    }
}
