using System.Threading.Tasks;
using Meshmakers.Octo.Common.Shared;
using Meshmakers.Octo.Communication.Sockets.Contracts.DataTransferObjects;
using Meshmakers.Octo.Communication.Sockets.Contracts.Hubs;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;

/// <summary>
/// Implementation the socket hub client proxy using SignalR of <see cref="ISocketHubClient"/>.
/// </summary>
public class SocketHubClient : SignalRClient<SocketHubClientOptions>, ISocketHubClient
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="clientOptions">Options for configuration of the client proxy.</param>
    /// <param name="serviceClientAccessToken">The access token management object</param>
    /// <param name="plugHubCallbacks">Callbacks for signalr communication</param>
    public SocketHubClient(IOptions<SocketHubClientOptions> clientOptions, IServiceClientAccessToken serviceClientAccessToken,
        ISocketHubCallbacks plugHubCallbacks) : this(clientOptions.Value, serviceClientAccessToken, plugHubCallbacks)
    {
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="clientOptions">Options for configuration of the client proxy.</param>
    /// <param name="serviceClientAccessToken">The access token management object</param>
    /// <param name="plugHubCallbacks">Callbacks for signalr communication</param>
    public SocketHubClient(SocketHubClientOptions clientOptions, IServiceClientAccessToken serviceClientAccessToken,
        ISocketHubCallbacks plugHubCallbacks) : base(
        clientOptions, serviceClientAccessToken, "socketHub")
    {
        HubConnection.On<string, SocketConfigurationDto>(nameof(ISocketHubCallbacks.SocketConfigurationUpdatedAsync),
            plugHubCallbacks.SocketConfigurationUpdatedAsync);
    }

    /// <inheritdoc />
    public async Task<SocketConfigurationDto> RegisterSocketAsync(OctoObjectId socketRtId)
    {
        return await HubConnection.InvokeAsync<SocketConfigurationDto>(nameof(ISocketHub.RegisterSocketAsync), socketRtId);
    }

    /// <inheritdoc />
    public async Task UnRegisterSocketAsync(OctoObjectId socketRtId)
    {
        await HubConnection.InvokeAsync(nameof(ISocketHub.UnRegisterSocketAsync), socketRtId);
    }
}