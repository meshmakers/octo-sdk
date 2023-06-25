using System.Threading.Tasks;
using Meshmakers.Octo.Common.Shared;
using Meshmakers.Octo.Communication.Sockets.Contracts.DataTransferObjects;
using Meshmakers.Octo.Communication.Sockets.Contracts.Hubs;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;

public class SocketHubClient : SignalRClient<SocketHubClientOptions>, ISocketHubClient
{
    public SocketHubClient(IOptions<SocketHubClientOptions> clientOptions, IServiceClientAccessToken serviceClientAccessToken,
        ISocketHubCallbacks plugHubCallbacks) : this(clientOptions.Value, serviceClientAccessToken, plugHubCallbacks)
    {
    }

    public SocketHubClient(SocketHubClientOptions clientOptions, IServiceClientAccessToken serviceClientAccessToken,
        ISocketHubCallbacks plugHubCallbacks) : base(
        clientOptions, serviceClientAccessToken, "socketHub")
    {
        HubConnection.On<string, SocketConfigurationDto>(nameof(ISocketHubCallbacks.SocketConfigurationUpdatedAsync),
            plugHubCallbacks.SocketConfigurationUpdatedAsync);
    }

    public async Task<SocketConfigurationDto> RegisterSocketAsync(OctoObjectId socketRtId)
    {
        return await HubConnection.InvokeAsync<SocketConfigurationDto>(nameof(ISocketHub.RegisterSocketAsync), socketRtId);
    }

    public async Task UnRegisterSocketAsync(OctoObjectId socketRtId)
    {
        await HubConnection.InvokeAsync(nameof(ISocketHub.UnRegisterSocketAsync), socketRtId);
    }
}