using System.Threading.Tasks;
using Meshmakers.Octo.Communication.Plugs.Contracts.DataTransferObjects;
using Meshmakers.Octo.Communication.Plugs.Contracts.Hubs;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;

namespace Meshmakers.Octo.Sdk.ServiceClient.PlugControllerServices;

public class PlugPoolControllerClient : SignalRClient, IPlugPoolControllerClient
{
    public PlugPoolControllerClient(IOptions<PlugControllerClientOptions> plugControllerServiceClientOptions,
        IPlugControllerServiceClientAccessToken plugControllerServiceAccessToken, IPoolHubCallbacks plugPoolHubCallbacks)
        : this(plugControllerServiceClientOptions.Value, plugControllerServiceAccessToken, plugPoolHubCallbacks)
    {
    }

    public PlugPoolControllerClient(PlugControllerClientOptions plugControllerServiceClientOptions,
        IPlugControllerServiceClientAccessToken plugControllerServiceAccessToken, IPoolHubCallbacks plugPoolHubCallbacks)
        : base(plugControllerServiceClientOptions, plugControllerServiceAccessToken, "plugPoolHub")
    {
        HubConnection.On<string, PlugPoolPlugDto>(nameof(IPoolHubCallbacks.DeployPlugAsync),
            plugPoolHubCallbacks.DeployPlugAsync);
        HubConnection.On<string, PlugPoolPlugDto>(nameof(IPoolHubCallbacks.UndeployPlugAsync),
            plugPoolHubCallbacks.UndeployPlugAsync);
    }

    public bool IsAlive => HubConnection.State != HubConnectionState.Disconnected;

    public async Task<PlugPoolConfigurationDto> RegisterPlugPoolOperatorAsync(string plugPoolName)
    {
        return await HubConnection.InvokeAsync<PlugPoolConfigurationDto>(nameof(IPoolHub.RegisterPlugPoolOperatorAsync), plugPoolName);
    }
    
    public async Task UnregisterPlugPoolOperatorAsync(string plugPoolName)
    {
        await HubConnection.InvokeAsync(nameof(IPoolHub.UnregisterPlugPoolOperatorAsync), plugPoolName);
    }
}