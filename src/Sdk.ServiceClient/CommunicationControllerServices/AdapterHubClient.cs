using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Communication.Contracts.Hubs;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
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
    /// <param name="logger">Logger instance</param>
    /// <param name="serviceClientAccessToken">The access token management object</param>
    /// <param name="adapterHubCallbacks">Callbacks for signalr communication</param>
    public AdapterHubClient(IOptions<AdapterHubClientOptions> options, ILogger<AdapterHubClient> logger,
        IServiceClientAccessToken serviceClientAccessToken, IAdapterHubCallbacks adapterHubCallbacks)
        : this(options.Value, logger, serviceClientAccessToken, adapterHubCallbacks)
    {
    }

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="adapterHubServiceClientOptions">Options for configuration of the client proxy.</param>
    /// <param name="logger">Instance of the logger</param>
    /// <param name="serviceClientAccessToken">The access token management object</param>
    /// <param name="adapterHubCallbacks">Callbacks for signalr communication</param>
    public AdapterHubClient(AdapterHubClientOptions adapterHubServiceClientOptions, ILogger<AdapterHubClient> logger,
        IServiceClientAccessToken serviceClientAccessToken, IAdapterHubCallbacks adapterHubCallbacks)
        : base(adapterHubServiceClientOptions, logger, serviceClientAccessToken, "adapterHub")
    {
        HubConnection.On<string, AdapterConfigurationDto>(nameof(IAdapterHubCallbacks.AdapterConfigurationUpdatedAsync),
            adapterHubCallbacks.AdapterConfigurationUpdatedAsync);
        HubConnection.On<string>(nameof(IAdapterHubCallbacks.PreUpdateTenantAsync),
            adapterHubCallbacks.PreUpdateTenantAsync);
    }

    /// <inheritdoc />
    public async Task<AdapterConfigurationDto> RegisterAdapterAsync(RtEntityId adapterRtEntityId)
    {
        return await HubConnection.InvokeAsync<AdapterConfigurationDto>(nameof(IAdapterHub.RegisterAdapterAsync), adapterRtEntityId);
    }

    /// <inheritdoc />
    public async Task<AdapterConfigurationDto> RegisterAdapterWithNodesAsync(RtEntityId adapterRtEntityId,
        IReadOnlyList<NodeDescriptorDto> nodeDescriptors)
    {
        return await HubConnection.InvokeAsync<AdapterConfigurationDto>(
            nameof(IAdapterHub.RegisterAdapterWithNodesAsync), adapterRtEntityId, nodeDescriptors);
    }

    /// <inheritdoc />
    public async Task<AdapterConfigurationDto> RegisterAdapterWithSchemaAsync(RtEntityId adapterRtEntityId,
        IReadOnlyList<NodeDescriptorDto> nodeDescriptors, string pipelineSchemaJson)
    {
        return await HubConnection.InvokeAsync<AdapterConfigurationDto>(
            nameof(IAdapterHub.RegisterAdapterWithSchemaAsync), adapterRtEntityId, nodeDescriptors, pipelineSchemaJson);
    }

    /// <inheritdoc />
    public async Task UnRegisterAdapterAsync(RtEntityId adapterRtEntityId)
    {
        await HubConnection.InvokeAsync(nameof(IAdapterHub.UnRegisterAdapterAsync), adapterRtEntityId);
    }

    /// <inheritdoc />
    public async Task SendDebugDataAsync(RtEntityId pipelineRtEntityId, Guid pipelineExecutionId, DebugPointDto debugPoint)
    {
        await HubConnection.InvokeAsync(nameof(IAdapterHub.SendDebugDataAsync), pipelineRtEntityId, pipelineExecutionId, debugPoint);
    }

    /// <inheritdoc />
    public async Task SendDeploymentUpdateResultAsync(RtEntityId adapterRtEntityId, DeploymentResult deploymentResult)
    {
        // Use SendAsync (fire-and-forget) instead of InvokeAsync (request-response) to avoid
        // blocking the adapter while the CC processes DB updates in the hub method.
        // InvokeAsync waits for the server hub method to fully complete, including all DB writes
        // in UpdateConfigurationStateAsync. If those are slow, the adapter hangs and the
        // configuration update lock is never released, blocking all subsequent config updates.
        await HubConnection.SendAsync(nameof(IAdapterHub.SendDeploymentUpdateResultAsync), adapterRtEntityId, deploymentResult);
    }

    /// <inheritdoc />
    public async Task ReportExecutionStartAsync(PipelineExecutionStartDto startDto)
    {
        // Use SendAsync (fire-and-forget) instead of InvokeAsync (request-response) to avoid
        // blocking the SignalR connection. Execution reports are informational and don't need
        // a server acknowledgement. This prevents execution reports from queuing up and
        // delaying higher-priority messages like deployment results.
        await HubConnection.SendAsync(nameof(IAdapterHub.ReportExecutionStartAsync), startDto);
    }

    /// <inheritdoc />
    public async Task ReportExecutionEndAsync(PipelineExecutionEndDto endDto)
    {
        // Use SendAsync (fire-and-forget) - see comment in ReportExecutionStartAsync.
        await HubConnection.SendAsync(nameof(IAdapterHub.ReportExecutionEndAsync), endDto);
    }

    /// <inheritdoc />
    public async Task ReportInterruptedExecutionResultAsync(PipelineExecutionEndDto endDto)
    {
        // Use SendAsync (fire-and-forget) - see comment in ReportExecutionStartAsync.
        await HubConnection.SendAsync(nameof(IAdapterHub.ReportInterruptedExecutionResultAsync), endDto);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetInterruptedExecutionIdsAsync()
    {
        return await HubConnection.InvokeAsync<IReadOnlyList<string>>(nameof(IAdapterHub.GetInterruptedExecutionIdsAsync));
    }

    /// <inheritdoc />
    public async Task<int> FailOrphanedExecutionsAsync(DateTime processStartUtc)
    {
        // Invoke (request/response) so the fresh process can wait for orphan resolution to complete
        // before it starts accepting/triggering new executions.
        return await HubConnection.InvokeAsync<int>(nameof(IAdapterHub.FailOrphanedExecutionsAsync), processStartUtc);
    }

    /// <inheritdoc />
    public async Task ReportAdapterMetricsAsync(AdapterMetricsSampleDto sample)
    {
        // Use SendAsync (fire-and-forget) - see comment in ReportExecutionStartAsync.
        // Sampling fires every few seconds; back-pressure or transient drops are
        // acceptable, the controller's ring buffer self-heals on the next sample.
        await HubConnection.SendAsync(nameof(IAdapterHub.ReportAdapterMetricsAsync), sample);
    }
}