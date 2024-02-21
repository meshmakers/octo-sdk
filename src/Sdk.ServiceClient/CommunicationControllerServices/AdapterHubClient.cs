using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Communication.Contracts.Hubs;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;

/// <summary>
///     Implementation the adapter hub client proxy using SignalR of <see cref="IAdapterHubClient" />.
/// </summary>
public class AdapterHubClient : SignalRClient<AdapterHubClientOptions>, IAdapterHubClient
{
    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="options">Options for configuration of the client proxy.</param>
    /// <param name="serviceClientAccessToken">The access token management object</param>
    /// <param name="adapterHubCallbacks">Callbacks for signalr communication</param>
    public AdapterHubClient(IOptions<AdapterHubClientOptions> options,
        IServiceClientAccessToken serviceClientAccessToken, IAdapterHubCallbacks adapterHubCallbacks)
        : this(options.Value, serviceClientAccessToken, adapterHubCallbacks)
    {
    }

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="adapterHubServiceClientOptions">Options for configuration of the client proxy.</param>
    /// <param name="serviceClientAccessToken">The access token management object</param>
    /// <param name="adapterHubCallbacks">Callbacks for signalr communication</param>
    public AdapterHubClient(AdapterHubClientOptions adapterHubServiceClientOptions,
        IServiceClientAccessToken serviceClientAccessToken, IAdapterHubCallbacks adapterHubCallbacks)
        : base(adapterHubServiceClientOptions, serviceClientAccessToken, "adapterHub")
    {
        HubConnection.On<string, AdapterConfigurationDto>(nameof(IAdapterHubCallbacks.AdapterConfigurationUpdatedAsync),
            adapterHubCallbacks.AdapterConfigurationUpdatedAsync);
    }

    /// <inheritdoc />
    public async Task<AdapterConfigurationDto> RegisterAdapterAsync(OctoObjectId adapterRtId)
    {
        return await HubConnection.InvokeAsync<AdapterConfigurationDto>(nameof(IAdapterHub.RegisterAdapterAsync), adapterRtId);
    }

    /// <inheritdoc />
    public async Task UnRegisterAdapterAsync(OctoObjectId adapterRtId)
    {
        await HubConnection.InvokeAsync(nameof(IAdapterHub.UnRegisterAdapterAsync), adapterRtId);
    }
}