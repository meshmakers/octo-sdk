using System.Threading.Tasks;
using Meshmakers.Octo.Common.Shared;
using Meshmakers.Octo.Communication.Plugs.Contracts.DataTransferObjects;
using Meshmakers.Octo.Communication.Plugs.Contracts.Hubs;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace Meshmakers.Octo.Sdk.Client.PlugControllerServices;

public class PlugControllerClient : SignalRClient, IPlugControllerClient
{
    public PlugControllerClient(IOptions<PlugControllerClientOptions> plugControllerServiceClientOptions,
        IPlugControllerServiceClientAccessToken plugControllerServiceAccessToken)
        : this(plugControllerServiceClientOptions.Value, plugControllerServiceAccessToken)
    {
    }

    public PlugControllerClient(PlugControllerClientOptions plugControllerServiceClientOptions,
        IPlugControllerServiceClientAccessToken plugControllerServiceAccessToken)
    : base(plugControllerServiceClientOptions, plugControllerServiceAccessToken, "plugHub")
    {
    }

    public async Task<PlugConfigurationDto> RegisterPlugAsync(OctoObjectId plugObjectId)
    {
        return await HubConnection.InvokeAsync<PlugConfigurationDto>(nameof(IPlugHub.RegisterPlug), plugObjectId);
    }
}