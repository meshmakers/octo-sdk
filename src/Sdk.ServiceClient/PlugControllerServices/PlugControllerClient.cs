using System.Threading.Tasks;
using Meshmakers.Octo.Common.Shared;
using Meshmakers.Octo.Communication.Plugs.Contracts.DataTransferObjects;
using Meshmakers.Octo.Communication.Plugs.Contracts.Hubs;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace Meshmakers.Octo.Sdk.ServiceClient.PlugControllerServices;

public class PlugControllerClient : SignalRClient, IPlugControllerClient
{
    public PlugControllerClient(IOptions<PlugControllerClientOptions> plugControllerServiceClientOptions,
        IPlugControllerServiceClientAccessToken plugControllerServiceAccessToken, IPlugHubCallbacks plugHubCallbacks)
        : this(plugControllerServiceClientOptions.Value, plugControllerServiceAccessToken, plugHubCallbacks)
    {
    }

    public PlugControllerClient(PlugControllerClientOptions plugControllerServiceClientOptions,
        IPlugControllerServiceClientAccessToken plugControllerServiceAccessToken, IPlugHubCallbacks plugHubCallbacks)
    : base(plugControllerServiceClientOptions, plugControllerServiceAccessToken, "plugHub")
    {
        HubConnection.On<string, PlugConfigurationDto>(nameof(IPlugHubCallbacks.PlugConfigurationUpdatedAsync),
            plugHubCallbacks.PlugConfigurationUpdatedAsync);
    }

    public async Task<PlugConfigurationDto> RegisterPlugAsync(OctoObjectId plugObjectId)
    {
        return await HubConnection.InvokeAsync<PlugConfigurationDto>(nameof(IPlugHub.RegisterPlugAsync), plugObjectId);
    }
}