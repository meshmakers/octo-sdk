using Meshmakers.Common.Shared;
using Meshmakers.Octo.Communication.Contracts;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;

/// <summary>
///     Implementation of the client proxy for communication controller services.
///     Accepts plain runtime object IDs and constructs composite RtEntityId strings where the server requires them.
/// </summary>
public class CommunicationServicesClient : ServiceClient, ICommunicationServicesClient
{
    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="serviceClientOptions">Options for configuration of the client proxy.</param>
    /// <param name="accessToken">The access token management object</param>
    public CommunicationServicesClient(IOptions<CommunicationServiceClientOptions> serviceClientOptions,
        ICommunicationServiceClientAccessToken accessToken)
        : this(serviceClientOptions.Value, accessToken)
    {
    }

    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="serviceClientOptions">Options for configuration of the client proxy.</param>
    /// <param name="accessToken">The access token management object</param>
    public CommunicationServicesClient(CommunicationServiceClientOptions serviceClientOptions,
        ICommunicationServiceClientAccessToken accessToken)
        : base(serviceClientOptions, accessToken)
    {
    }

    /// <inheritdoc />
    protected override Uri BuildServiceUri()
    {
        if (string.IsNullOrWhiteSpace(Options.EndpointUri))
        {
            throw new ServiceConfigurationMissingException("Communication services URI is missing");
        }

        var communicationOptions = (CommunicationServiceClientOptions)Options;
        if (string.IsNullOrWhiteSpace(communicationOptions.TenantId))
        {
            throw new ServiceConfigurationMissingException("Communication services tenant ID is missing");
        }

        return new Uri(Options.EndpointUri).Append(communicationOptions.TenantId!).Append("v1");
    }

    // ── Enable / Disable ──────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task EnableAsync(string tenantId)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);

        var request = new RestRequest("communication/enable", Method.Post);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task DisableAsync(string tenantId)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);

        var request = new RestRequest("communication/disable", Method.Post);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task ReconfigureLogLevelAsync(string loggerName, LogLevelDto minLogLevel, LogLevelDto maxLogLevel)
    {
        // The DiagnosticsController is system-scoped (system/v1/diagnostics). The client base URI is
        // tenant-scoped ({tenantId}/v1), so target the diagnostics endpoint via an absolute system URL
        // (RestSharp uses an absolute resource URL as-is instead of combining it with the base URI).
        var diagnosticsUri = new Uri(Options.EndpointUri!).Append("system", "v1", "diagnostics", "reconfigureLogLevel");
        var request = new RestRequest(diagnosticsUri, Method.Post);
        request.AddQueryParameter("loggerName", loggerName);
        request.AddQueryParameter("minLogLevel", minLogLevel);
        request.AddQueryParameter("maxLogLevel", maxLogLevel);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    // ── Adapters ──────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdapterSummaryDto>> GetAdaptersAsync()
    {
        var request = new RestRequest("adapter");

        var response = await Client.ExecuteAsync<List<AdapterSummaryDto>>(request);
        ValidateResponse(response);

        return response.Data ?? [];
    }

    /// <inheritdoc />
    public async Task<AdapterConfigurationDto> GetAdapterConfigurationAsync(string adapterRtId)
    {
        ArgumentValidation.ValidateString(nameof(adapterRtId), adapterRtId);

        var adapterRtEntityId = CommunicationCkTypeIds.ToCompositeId(CommunicationCkTypeIds.Adapter, adapterRtId);
        var request = new RestRequest("adapter/{adapterRtEntityId}");
        request.AddUrlSegment("adapterRtEntityId", adapterRtEntityId);
        request.AddQueryParameter("adapterRtEntityId", adapterRtEntityId);

        var response = await Client.ExecuteAsync<AdapterConfigurationDto>(request);
        ValidateResponse(response);

        return response.Data!;
    }

    /// <inheritdoc />
    public async Task<string> GetAdapterNodesAsync()
    {
        var request = new RestRequest("adapter/nodes");

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);

        return response.Content ?? "[]";
    }

    /// <inheritdoc />
    public async Task<string> GetPipelineSchemaAsync(string adapterRtId)
    {
        ArgumentValidation.ValidateString(nameof(adapterRtId), adapterRtId);

        var adapterRtEntityId = CommunicationCkTypeIds.ToCompositeId(CommunicationCkTypeIds.Adapter, adapterRtId);
        var request = new RestRequest("adapter/pipeline-schema");
        request.AddQueryParameter("adapterRtEntityId", adapterRtEntityId);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);

        return response.Content ?? "{}";
    }

    // ── Pipelines ─────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<DeploymentResultDto> GetPipelineDeploymentStateAsync(string pipelineRtId)
    {
        ArgumentValidation.ValidateString(nameof(pipelineRtId), pipelineRtId);

        var pipelineRtEntityId =
            CommunicationCkTypeIds.ToCompositeId(CommunicationCkTypeIds.Pipeline, pipelineRtId);
        var request = new RestRequest("pipeline/status");
        request.AddQueryParameter("pipelineRtEntityId", pipelineRtEntityId);

        var response = await Client.ExecuteAsync<DeploymentResultDto>(request);
        ValidateResponse(response);

        return response.Data!;
    }

    /// <inheritdoc />
    public async Task DeployPipelineAsync(string adapterRtId, string pipelineRtId,
        string pipelineDefinition)
    {
        ArgumentValidation.ValidateString(nameof(adapterRtId), adapterRtId);
        ArgumentValidation.ValidateString(nameof(pipelineRtId), pipelineRtId);
        ArgumentValidation.ValidateString(nameof(pipelineDefinition), pipelineDefinition);

        var adapterRtEntityId = CommunicationCkTypeIds.ToCompositeId(CommunicationCkTypeIds.Adapter, adapterRtId);
        var pipelineRtEntityId =
            CommunicationCkTypeIds.ToCompositeId(CommunicationCkTypeIds.Pipeline, pipelineRtId);
        var request = new RestRequest("pipeline/deploy", Method.Post);
        request.AddQueryParameter("adapterRtEntityId", adapterRtEntityId);
        request.AddQueryParameter("pipelineRtEntityId", pipelineRtEntityId);
        request.AddStringBody(pipelineDefinition, ContentType.Plain);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task<string> ExecutePipelineAsync(string pipelineRtId, string? pipelineInput,
        bool isDryRun = false)
    {
        ArgumentValidation.ValidateString(nameof(pipelineRtId), pipelineRtId);

        var request = new RestRequest("pipeline/execute", Method.Post);
        request.AddQueryParameter("pipelineRtId", pipelineRtId);
        if (isDryRun)
        {
            request.AddQueryParameter("isDryRun", "true");
        }

        if (!string.IsNullOrEmpty(pipelineInput))
        {
            request.AddStringBody(pipelineInput!, ContentType.Plain);
        }

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);

        return response.Content ?? string.Empty;
    }

    // ── Pipeline Debug ────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<IEnumerable<PipelineExecutionDataDto>> GetPipelineExecutionsAsync(string pipelineRtId)
    {
        ArgumentValidation.ValidateString(nameof(pipelineRtId), pipelineRtId);

        var pipelineRtEntityId =
            CommunicationCkTypeIds.ToCompositeId(CommunicationCkTypeIds.Pipeline, pipelineRtId);
        var request = new RestRequest("pipelinedebug/{pipelineRtEntityId}");
        request.AddUrlSegment("pipelineRtEntityId", pipelineRtEntityId);

        var response = await Client.ExecuteAsync<List<PipelineExecutionDataDto>>(request);
        ValidateResponse(response);

        return response.Data ?? new List<PipelineExecutionDataDto>();
    }

    /// <inheritdoc />
    public async Task<PipelineExecutionDataDto> GetLatestPipelineExecutionAsync(string pipelineRtId)
    {
        ArgumentValidation.ValidateString(nameof(pipelineRtId), pipelineRtId);

        var pipelineRtEntityId =
            CommunicationCkTypeIds.ToCompositeId(CommunicationCkTypeIds.Pipeline, pipelineRtId);
        var request = new RestRequest("pipelinedebug/{pipelineRtEntityId}/latest");
        request.AddUrlSegment("pipelineRtEntityId", pipelineRtEntityId);

        var response = await Client.ExecuteAsync<PipelineExecutionDataDto>(request);
        ValidateResponse(response);

        return response.Data!;
    }

    /// <inheritdoc />
    public async Task<string> GetPipelineExecutionDebugPointsAsync(string pipelineRtId, Guid executionId)
    {
        ArgumentValidation.ValidateString(nameof(pipelineRtId), pipelineRtId);

        var pipelineRtEntityId =
            CommunicationCkTypeIds.ToCompositeId(CommunicationCkTypeIds.Pipeline, pipelineRtId);
        var request = new RestRequest("pipelinedebug/{pipelineRtEntityId}/{executionId}");
        request.AddUrlSegment("pipelineRtEntityId", pipelineRtEntityId);
        request.AddUrlSegment("executionId", executionId.ToString());

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);

        return response.Content ?? "[]";
    }

    /// <inheritdoc />
    public async Task<DebugPointDataDto> GetDebugPointAsync(string pipelineRtId, Guid executionId,
        string nodeId)
    {
        ArgumentValidation.ValidateString(nameof(pipelineRtId), pipelineRtId);
        ArgumentValidation.ValidateString(nameof(nodeId), nodeId);

        var pipelineRtEntityId =
            CommunicationCkTypeIds.ToCompositeId(CommunicationCkTypeIds.Pipeline, pipelineRtId);
        var request = new RestRequest("pipelinedebug/{pipelineRtEntityId}/{executionId}/{nodeId}");
        request.AddUrlSegment("pipelineRtEntityId", pipelineRtEntityId);
        request.AddUrlSegment("executionId", executionId.ToString());
        request.AddUrlSegment("nodeId", nodeId);

        var response = await Client.ExecuteAsync<DebugPointDataDto>(request);
        ValidateResponse(response);

        return response.Data!;
    }

    // ── Triggers ──────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task DeployTriggersAsync()
    {
        var request = new RestRequest("pipelinetrigger/deploy", Method.Post);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task UndeployTriggersAsync()
    {
        var request = new RestRequest("pipelinetrigger/undeploy", Method.Post);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    // ── Pools ─────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<IReadOnlyList<PoolSummaryDto>> GetPoolsAsync()
    {
        var request = new RestRequest("pool");

        var response = await Client.ExecuteAsync<List<PoolSummaryDto>>(request);
        ValidateResponse(response);

        return response.Data ?? [];
    }

    // ── Data Flows ────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task DeployDataFlowAsync(string dataFlowRtId)
    {
        ArgumentValidation.ValidateString(nameof(dataFlowRtId), dataFlowRtId);

        var request = new RestRequest("dataflow/deploy", Method.Post);
        request.AddQueryParameter("dataFlowRtId", dataFlowRtId);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task<SetPipelineDebugResultDto> SetPipelineDebuggingAsync(string pipelineRtId, bool enabled)
    {
        ArgumentValidation.ValidateString(nameof(pipelineRtId), pipelineRtId);

        var request = new RestRequest("pipeline/{pipelineRtId}/debug", Method.Patch);
        request.AddUrlSegment("pipelineRtId", pipelineRtId);
        request.AddJsonBody(new SetPipelineDebugRequestDto(enabled));

        var response = await Client.ExecuteAsync<SetPipelineDebugResultDto>(request);
        ValidateResponse(response);

        return response.Data ?? new SetPipelineDebugResultDto(enabled, false);
    }

    /// <inheritdoc />
    public async Task<PipelineDebugStateDto> GetPipelineDebuggingAsync(string pipelineRtId)
    {
        ArgumentValidation.ValidateString(nameof(pipelineRtId), pipelineRtId);

        var request = new RestRequest("pipeline/{pipelineRtId}/debug", Method.Get);
        request.AddUrlSegment("pipelineRtId", pipelineRtId);

        var response = await Client.ExecuteAsync<PipelineDebugStateDto>(request);
        ValidateResponse(response);

        return response.Data ?? new PipelineDebugStateDto(false);
    }

    /// <inheritdoc />
    public async Task UndeployDataFlowAsync(string dataFlowRtId)
    {
        ArgumentValidation.ValidateString(nameof(dataFlowRtId), dataFlowRtId);

        var request = new RestRequest("dataflow/undeploy", Method.Post);
        request.AddQueryParameter("dataFlowRtId", dataFlowRtId);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task<DataFlowStatusDto> GetDataFlowStatusAsync(string dataFlowRtId)
    {
        ArgumentValidation.ValidateString(nameof(dataFlowRtId), dataFlowRtId);

        var request = new RestRequest("dataflow/status");
        request.AddQueryParameter("dataFlowRtId", dataFlowRtId);

        var response = await Client.ExecuteAsync<DataFlowStatusDto>(request);
        ValidateResponse(response);

        return response.Data!;
    }

    // ── Workload chart management (Epic 3054, Phase 2 — #4052) ──────────────

    /// <inheritdoc />
    public async Task<IReadOnlyList<WorkloadSummaryDto>> GetWorkloadsByChartAsync(string chartName)
    {
        ArgumentValidation.ValidateString(nameof(chartName), chartName);

        var request = new RestRequest("workload");
        request.AddQueryParameter("chartName", chartName);

        var response = await Client.ExecuteAsync<List<WorkloadSummaryDto>>(request);
        ValidateResponse(response);

        return response.Data ?? new List<WorkloadSummaryDto>();
    }

    /// <inheritdoc />
    public async Task UpdateWorkloadChartVersionAsync(string workloadRtId, string chartVersion)
    {
        ArgumentValidation.ValidateString(nameof(workloadRtId), workloadRtId);
        ArgumentValidation.ValidateString(nameof(chartVersion), chartVersion);

        var request = new RestRequest("workload/{workloadRtId}/chart-version", Method.Patch);
        request.AddUrlSegment("workloadRtId", workloadRtId);
        request.AddJsonBody(new UpdateChartVersionDto(chartVersion));

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task DeployPoolAsync(string poolRtId)
    {
        ArgumentValidation.ValidateString(nameof(poolRtId), poolRtId);

        var request = new RestRequest("pool/deploy", Method.Post);
        request.AddQueryParameter("poolRtId", poolRtId);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task DeployWorkloadAsync(string workloadRtId)
    {
        ArgumentValidation.ValidateString(nameof(workloadRtId), workloadRtId);

        var request = new RestRequest("pool/workloads/deploy", Method.Post);
        request.AddQueryParameter("workloadRtId", workloadRtId);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task UndeployWorkloadAsync(string workloadRtId)
    {
        ArgumentValidation.ValidateString(nameof(workloadRtId), workloadRtId);

        var request = new RestRequest("pool/workloads/undeploy", Method.Post);
        request.AddQueryParameter("workloadRtId", workloadRtId);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    // ── Pipeline reassignment ───────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<MovePipelinesToAdapterResponseDto> MovePipelinesToAdapterAsync(
        MovePipelinesToAdapterRequestDto requestDto)
    {
        if (requestDto == null) throw new ArgumentNullException(nameof(requestDto));
        ArgumentValidation.ValidateString(nameof(requestDto.TargetAdapterRtId), requestDto.TargetAdapterRtId);
        if (requestDto.PipelineRtIds.Count == 0)
        {
            throw new ArgumentException("At least one pipelineRtId must be supplied", nameof(requestDto));
        }

        var request = new RestRequest("pipeline/move-to-adapter", Method.Patch);
        request.AddJsonBody(requestDto);

        var response = await Client.ExecuteAsync<MovePipelinesToAdapterResponseDto>(request);
        ValidateResponse(response);

        return response.Data
               ?? new MovePipelinesToAdapterResponseDto(new List<MovePipelineResultDto>());
    }
}
