using System.Threading.Tasks;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Communication.Contracts.Hubs;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;

namespace Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;

public class PoolHubClient : SignalRClient<PoolHubClientOptions>, IPoolHubClient
{
    public PoolHubClient(IOptions<PoolHubClientOptions> poolControllerServiceClientOptions,
        IServiceClientAccessToken serviceClientAccessToken, IPoolHubCallbacks plugPoolHubCallbacks)
        : this(poolControllerServiceClientOptions.Value, serviceClientAccessToken, plugPoolHubCallbacks)
    {
    }

    public PoolHubClient(PoolHubClientOptions poolHubServiceClientOptions,
        IServiceClientAccessToken serviceClientAccessToken, IPoolHubCallbacks plugPoolHubCallbacks)
        : base(poolHubServiceClientOptions, serviceClientAccessToken, "plugPoolHub")
    {
        HubConnection.On<string, PoolPlugDto>(nameof(IPoolHubCallbacks.DeployPlugAsync),
            plugPoolHubCallbacks.DeployPlugAsync);
        HubConnection.On<string, PoolPlugDto>(nameof(IPoolHubCallbacks.UndeployPlugAsync),
            plugPoolHubCallbacks.UndeployPlugAsync);
    }

    public bool IsAlive => HubConnection.State != HubConnectionState.Disconnected;

    public async Task<PoolConfigurationDto> RegisterPlugPoolOperatorAsync(string plugPoolName)
    {
        return await HubConnection.InvokeAsync<PoolConfigurationDto>(nameof(IPoolHub.RegisterPlugPoolOperatorAsync), plugPoolName);
    }

    public async Task UnregisterPlugPoolOperatorAsync(string plugPoolName)
    {
        await HubConnection.InvokeAsync(nameof(IPoolHub.UnregisterPlugPoolOperatorAsync), plugPoolName);
    }
}
