using System.Threading.Tasks;
using Meshmakers.Octo.Common.Shared;
using Meshmakers.Octo.Communication.Plugs.Contracts.DataTransferObjects;
using Meshmakers.Octo.Communication.Plugs.Contracts.Hubs;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;

public class PlugHubClient : SignalRClient<PlugHubClientOptions>, IPlugHubClient
{
    public PlugHubClient(IOptions<PlugHubClientOptions> plugControllerServiceClientOptions,
        IServiceClientAccessToken serviceClientAccessToken, IPlugHubCallbacks plugHubCallbacks)
        : this(plugControllerServiceClientOptions.Value, serviceClientAccessToken, plugHubCallbacks)
    {
    }

    public PlugHubClient(PlugHubClientOptions plugHubServiceClientOptions,
        IServiceClientAccessToken serviceClientAccessToken, IPlugHubCallbacks plugHubCallbacks)
        : base(plugHubServiceClientOptions, serviceClientAccessToken, "plugHub")
    {
        HubConnection.On<string, PlugConfigurationDto>(nameof(IPlugHubCallbacks.PlugConfigurationUpdatedAsync),
            plugHubCallbacks.PlugConfigurationUpdatedAsync);
    }

    public async Task<PlugConfigurationDto> RegisterPlugAsync(OctoObjectId plugObjectId)
    {
        return await HubConnection.InvokeAsync<PlugConfigurationDto>(nameof(IPlugHub.RegisterPlugAsync), plugObjectId);
    }
}
