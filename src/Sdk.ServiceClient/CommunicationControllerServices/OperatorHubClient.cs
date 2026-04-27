using Meshmakers.Octo.Communication.Contracts.Hubs;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;

/// <summary>
/// Client proxy for the operator management hub of communication controller services.
/// Receives tenant lifecycle notifications from the controller.
/// </summary>
public class OperatorHubClient : SignalRClient<OperatorHubClientOptions>, IOperatorHubClient
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="serviceClientOptions">Options for configuration of the client proxy.</param>
    /// <param name="logger">Instance of the logger</param>
    /// <param name="serviceClientAccessToken">The access token management object</param>
    /// <param name="operatorHubCallbacks">Callbacks for tenant lifecycle notifications</param>
    public OperatorHubClient(OperatorHubClientOptions serviceClientOptions, ILogger<OperatorHubClient> logger,
        IServiceClientAccessToken serviceClientAccessToken, IOperatorHubCallbacks operatorHubCallbacks)
        : base(serviceClientOptions, logger, serviceClientAccessToken, "operatorHub")
    {
        HubConnection.On<string>(nameof(IOperatorHubCallbacks.TenantCreatedAsync),
            operatorHubCallbacks.TenantCreatedAsync);
        HubConnection.On<string>(nameof(IOperatorHubCallbacks.TenantDeletedAsync),
            operatorHubCallbacks.TenantDeletedAsync);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> RegisterOperatorAsync()
    {
        return await HubConnection.InvokeAsync<IEnumerable<string>>(nameof(IOperatorHub.RegisterOperatorAsync));
    }

    /// <inheritdoc />
    public async Task UnregisterOperatorAsync()
    {
        await HubConnection.InvokeAsync(nameof(IOperatorHub.UnregisterOperatorAsync));
    }
}
