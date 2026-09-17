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
    private readonly IOperatorHubCallbacks _operatorHubCallbacks;

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
        _operatorHubCallbacks = operatorHubCallbacks;
    }

    /// <summary>
    /// Binds the server-to-client callbacks on every (re-)created connection so they survive a
    /// StopAsync/StartAsync cycle. Registering them in the constructor only bound them to the
    /// first connection; after a full restart the fresh connection had no handlers and every
    /// pool/workload deploy notification from the controller was silently dropped.
    /// </summary>
    protected override void RegisterServerCallbacks(HubConnection hubConnection)
    {
        hubConnection.On<DeployedDeploymentSiteDto>(nameof(IOperatorHubCallbacks.DeploymentSiteDeployedAsync),
            _operatorHubCallbacks.DeploymentSiteDeployedAsync);
        hubConnection.On<string, string>(nameof(IOperatorHubCallbacks.DeploymentSiteUndeployedAsync),
            _operatorHubCallbacks.DeploymentSiteUndeployedAsync);
        hubConnection.On<WorkloadDeployedDto>(nameof(IOperatorHubCallbacks.WorkloadDeployedAsync),
            _operatorHubCallbacks.WorkloadDeployedAsync);
        hubConnection.On<WorkloadUndeployedDto>(nameof(IOperatorHubCallbacks.WorkloadUndeployedAsync),
            _operatorHubCallbacks.WorkloadUndeployedAsync);
        hubConnection.On<ScaleWorkloadDto>(nameof(IOperatorHubCallbacks.ScaleWorkloadAsync),
            _operatorHubCallbacks.ScaleWorkloadAsync);
        hubConnection.On<string>(nameof(IOperatorHubCallbacks.PreUpdateTenantAsync),
            _operatorHubCallbacks.PreUpdateTenantAsync);
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
    public async Task<IEnumerable<DeployedDeploymentSiteDto>> RegisterOperatorAsync(bool? autoManageDeploymentSites = null)
    {
        return await HubConnection.InvokeAsync<IEnumerable<DeployedDeploymentSiteDto>>(
            nameof(IOperatorHub.RegisterOperatorAsync), autoManageDeploymentSites);
    }

    /// <inheritdoc />
    public async Task UnregisterOperatorAsync()
    {
        await HubConnection.InvokeAsync(nameof(IOperatorHub.UnregisterOperatorAsync));
    }

    /// <inheritdoc />
    public async Task ReportDeployedStateAsync(IReadOnlyList<OperatorDeployedDeploymentSiteReportDto> deployedDeploymentSites)
    {
        await HubConnection.InvokeAsync(nameof(IOperatorHub.ReportDeployedStateAsync), deployedDeploymentSites);
    }

    /// <inheritdoc />
    public async Task ReportWorkloadDeploymentStatusAsync(WorkloadDeploymentStatusDto status)
    {
        await HubConnection.InvokeAsync(nameof(IOperatorHub.ReportWorkloadDeploymentStatusAsync), status);
    }

    /// <inheritdoc />
    public async Task ReportWorkloadDeploymentProgressAsync(WorkloadDeploymentProgressDto progress)
    {
        await HubConnection.InvokeAsync(nameof(IOperatorHub.ReportWorkloadDeploymentProgressAsync), progress);
    }

    /// <inheritdoc />
    public async Task ReportWorkloadScaleStatusAsync(WorkloadScaleStatusDto status)
    {
        await HubConnection.InvokeAsync(nameof(IOperatorHub.ReportWorkloadScaleStatusAsync), status);
    }

    /// <inheritdoc />
    public async Task RegisterDeploymentSiteAsync(string tenantId, string deploymentSiteRtId)
    {
        await HubConnection.InvokeAsync(nameof(IOperatorHub.RegisterDeploymentSiteAsync),
            tenantId, deploymentSiteRtId);
    }

    /// <inheritdoc />
    public async Task UnregisterDeploymentSiteAsync(string tenantId, string deploymentSiteRtId)
    {
        await HubConnection.InvokeAsync(nameof(IOperatorHub.UnregisterDeploymentSiteAsync),
            tenantId, deploymentSiteRtId);
    }
}
