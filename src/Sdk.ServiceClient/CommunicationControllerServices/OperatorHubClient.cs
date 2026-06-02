using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Communication.Contracts.Hubs;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;

/// <summary>
/// Client proxy for the operator management hub of communication controller services.
/// Receives Cloud pool deploy / undeploy notifications from the controller.
/// </summary>
public class OperatorHubClient : SignalRClient<OperatorHubClientOptions>, IOperatorHubClient
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="serviceClientOptions">Options for configuration of the client proxy.</param>
    /// <param name="logger">Instance of the logger</param>
    /// <param name="serviceClientAccessToken">The access token management object</param>
    /// <param name="operatorHubCallbacks">Callbacks for pool deploy / undeploy notifications</param>
    public OperatorHubClient(OperatorHubClientOptions serviceClientOptions, ILogger<OperatorHubClient> logger,
        IServiceClientAccessToken serviceClientAccessToken, IOperatorHubCallbacks operatorHubCallbacks)
        : base(serviceClientOptions, logger, serviceClientAccessToken, "operatorHub")
    {
        HubConnection.On<DeployedPoolDto>(nameof(IOperatorHubCallbacks.PoolDeployedAsync),
            operatorHubCallbacks.PoolDeployedAsync);
        HubConnection.On<string, string>(nameof(IOperatorHubCallbacks.PoolUndeployedAsync),
            operatorHubCallbacks.PoolUndeployedAsync);
        HubConnection.On<WorkloadDeployedDto>(nameof(IOperatorHubCallbacks.WorkloadDeployedAsync),
            operatorHubCallbacks.WorkloadDeployedAsync);
        HubConnection.On<WorkloadUndeployedDto>(nameof(IOperatorHubCallbacks.WorkloadUndeployedAsync),
            operatorHubCallbacks.WorkloadUndeployedAsync);
        HubConnection.On<string>(nameof(IOperatorHubCallbacks.PreUpdateTenantAsync),
            operatorHubCallbacks.PreUpdateTenantAsync);
    }

    /// <summary>
    /// Builds the service URI without tenant ID prefix (OperatorHub is not tenant-scoped).
    /// </summary>
    protected override Uri BuildServiceUri()
    {
        if (string.IsNullOrWhiteSpace(Options.EndpointUri))
        {
            throw new ServiceConfigurationMissingException("Communication Controller service URI is not configured.");
        }

        return new Uri(Options.EndpointUri).Append("operatorHub");
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DeployedPoolDto>> RegisterOperatorAsync(bool? autoManagePools = null)
    {
        return await HubConnection.InvokeAsync<IEnumerable<DeployedPoolDto>>(
            nameof(IOperatorHub.RegisterOperatorAsync), autoManagePools);
    }

    /// <inheritdoc />
    public async Task UnregisterOperatorAsync()
    {
        await HubConnection.InvokeAsync(nameof(IOperatorHub.UnregisterOperatorAsync));
    }

    /// <inheritdoc />
    public async Task ReportDeployedStateAsync(IReadOnlyList<OperatorDeployedPoolReportDto> deployedPools)
    {
        await HubConnection.InvokeAsync(nameof(IOperatorHub.ReportDeployedStateAsync), deployedPools);
    }

    /// <inheritdoc />
    public async Task ReportWorkloadDeploymentStatusAsync(WorkloadDeploymentStatusDto status)
    {
        await HubConnection.InvokeAsync(nameof(IOperatorHub.ReportWorkloadDeploymentStatusAsync), status);
    }

    /// <inheritdoc />
    public async Task RegisterPoolAsync(string tenantId, string poolRtId)
    {
        await HubConnection.InvokeAsync(nameof(IOperatorHub.RegisterPoolAsync),
            tenantId, poolRtId);
    }

    /// <inheritdoc />
    public async Task UnregisterPoolAsync(string tenantId, string poolRtId)
    {
        await HubConnection.InvokeAsync(nameof(IOperatorHub.UnregisterPoolAsync),
            tenantId, poolRtId);
    }
}
