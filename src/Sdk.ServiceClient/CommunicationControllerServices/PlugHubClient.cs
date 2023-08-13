using Meshmakers.Octo.Common.Shared;
using Meshmakers.Octo.Communication.Plugs.Contracts.DataTransferObjects;
using Meshmakers.Octo.Communication.Plugs.Contracts.Hubs;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;

/// <summary>
/// Implementation the plug hub client proxy using SignalR of <see cref="IPlugHubClient"/>.
/// </summary>
public class PlugHubClient : SignalRClient<PlugHubClientOptions>, IPlugHubClient
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="plugControllerServiceClientOptions">Options for configuration of the client proxy.</param>
    /// <param name="serviceClientAccessToken">The access token management object</param>
    /// <param name="plugHubCallbacks">Callbacks for signalr communication</param>
    public PlugHubClient(IOptions<PlugHubClientOptions> plugControllerServiceClientOptions,
        IServiceClientAccessToken serviceClientAccessToken, IPlugHubCallbacks plugHubCallbacks)
        : this(plugControllerServiceClientOptions.Value, serviceClientAccessToken, plugHubCallbacks)
    {
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="plugHubServiceClientOptions">Options for configuration of the client proxy.</param>
    /// <param name="serviceClientAccessToken">The access token management object</param>
    /// <param name="plugHubCallbacks">Callbacks for signalr communication</param>
    public PlugHubClient(PlugHubClientOptions plugHubServiceClientOptions,
        IServiceClientAccessToken serviceClientAccessToken, IPlugHubCallbacks plugHubCallbacks)
        : base(plugHubServiceClientOptions, serviceClientAccessToken, "plugHub")
    {
        HubConnection.On<string, PlugConfigurationDto>(nameof(IPlugHubCallbacks.PlugConfigurationUpdatedAsync),
            plugHubCallbacks.PlugConfigurationUpdatedAsync);
    }

    /// <inheritdoc />
    public async Task<PlugConfigurationDto> RegisterPlugAsync(OctoObjectId plugObjectId)
    {
        return await HubConnection.InvokeAsync<PlugConfigurationDto>(nameof(IPlugHub.RegisterPlugAsync), plugObjectId);
    }

    /// <inheritdoc />
    public async Task UnRegisterPlugAsync(OctoObjectId plugRtId)
    {
        await HubConnection.InvokeAsync(nameof(IPlugHub.UnRegisterPlugAsync), plugRtId);
    }
}