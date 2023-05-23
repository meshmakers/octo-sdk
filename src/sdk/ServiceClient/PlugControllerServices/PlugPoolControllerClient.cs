using System.Threading.Tasks;
using Meshmakers.Octo.Communication.Plugs.Contracts.DataTransferObjects;
using Meshmakers.Octo.Communication.Plugs.Contracts.Hubs;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;

namespace Meshmakers.Octo.Sdk.Client.PlugControllerServices;

public class PlugPoolControllerClient : SignalRClient, IPlugPoolControllerClient
{
    public PlugPoolControllerClient(IOptions<PlugControllerClientOptions> plugControllerServiceClientOptions,
        IPlugControllerServiceClientAccessToken plugControllerServiceAccessToken)
        : this(plugControllerServiceClientOptions.Value, plugControllerServiceAccessToken)
    {
    }

    public PlugPoolControllerClient(PlugControllerClientOptions plugControllerServiceClientOptions,
        IPlugControllerServiceClientAccessToken plugControllerServiceAccessToken)
        : base(plugControllerServiceClientOptions, plugControllerServiceAccessToken, "plugPoolHub")
    {
    }
    
    public async Task<PlugPoolConfigurationDto> RegisterPlugPoolAsync(string plugPoolName)
    {
        return await HubConnection.InvokeAsync<PlugPoolConfigurationDto>(nameof(IPlugPoolHub.RegisterPlugPool), plugPoolName);
    }
}