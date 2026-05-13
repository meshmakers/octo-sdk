using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

namespace Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;

/// <summary>
///     Client proxy for communication controller services.
///     All entity ID parameters accept plain runtime object IDs (e.g. "69cfa838092b710403248acd").
///     The client internally constructs composite RtEntityId strings where the server requires them.
/// </summary>
public interface ICommunicationServicesClient : IServiceClient
{
    /// <summary>
    ///     Enables the communication controller for a tenant
    /// </summary>
    /// <param name="tenantId">The id of the tenant.</param>
    Task EnableAsync(string tenantId);

    /// <summary>
    ///     Disables the communication controller for a tenant
    /// </summary>
    /// <param name="tenantId">The id of the tenant.</param>
    Task DisableAsync(string tenantId);

    /// <summary>
    ///     Reconfigure the log level of the service.
    /// </summary>
    /// <param name="loggerName">Logger pattern name, e. g. Microsoft.*</param>
    /// <param name="minLogLevel">Minimal log level to be logged.</param>
    /// <param name="maxLogLevel">Maximum log level to be logged.</param>
    Task ReconfigureLogLevelAsync(string loggerName, LogLevelDto minLogLevel, LogLevelDto maxLogLevel);

    // ── Adapters ──────────────────────────────────────────────────────────

    /// <summary>
    ///     Returns a list of all adapters for the tenant.
    /// </summary>
    Task<IReadOnlyList<AdapterSummaryDto>> GetAdaptersAsync();

    /// <summary>
    ///     Returns the configuration for a specific adapter.
    /// </summary>
    /// <param name="adapterRtId">The adapter runtime object ID.</param>
    Task<AdapterConfigurationDto> GetAdapterConfigurationAsync(string adapterRtId);

    /// <summary>
    ///     Returns aggregated node descriptors from all connected adapters as JSON.
    /// </summary>
    Task<string> GetAdapterNodesAsync();

    /// <summary>
    ///     Returns the composite pipeline JSON Schema for a specific adapter.
    /// </summary>
    /// <param name="adapterRtId">The adapter runtime object ID.</param>
    Task<string> GetPipelineSchemaAsync(string adapterRtId);

    // ── Pipelines ─────────────────────────────────────────────────────────

    /// <summary>
    ///     Gets the deployment state of a pipeline.
    /// </summary>
    /// <param name="pipelineRtId">The pipeline runtime object ID.</param>
    Task<DeploymentResultDto> GetPipelineDeploymentStateAsync(string pipelineRtId);

    /// <summary>
    ///     Deploys a pipeline definition to the corresponding adapter.
    /// </summary>
    /// <param name="adapterRtId">The adapter runtime object ID.</param>
    /// <param name="pipelineRtId">The pipeline runtime object ID.</param>
    /// <param name="pipelineDefinition">The pipeline definition (YAML/JSON).</param>
    Task DeployPipelineAsync(string adapterRtId, string pipelineRtId, string pipelineDefinition);

    /// <summary>
    ///     Executes a pipeline and returns the execution ID.
    /// </summary>
    /// <param name="pipelineRtId">The pipeline runtime object ID.</param>
    /// <param name="pipelineInput">Optional pipeline input data.</param>
    Task<string> ExecutePipelineAsync(string pipelineRtId, string? pipelineInput);

    // ── Pipeline Debug ────────────────────────────────────────────────────

    /// <summary>
    ///     Returns pipeline execution history.
    /// </summary>
    /// <param name="pipelineRtId">The pipeline runtime object ID.</param>
    Task<IEnumerable<PipelineExecutionDataDto>> GetPipelineExecutionsAsync(string pipelineRtId);

    /// <summary>
    ///     Returns the latest pipeline execution.
    /// </summary>
    /// <param name="pipelineRtId">The pipeline runtime object ID.</param>
    Task<PipelineExecutionDataDto> GetLatestPipelineExecutionAsync(string pipelineRtId);

    /// <summary>
    ///     Returns debug point nodes for a specific execution as JSON.
    /// </summary>
    /// <param name="pipelineRtId">The pipeline runtime object ID.</param>
    /// <param name="executionId">The execution id.</param>
    Task<string> GetPipelineExecutionDebugPointsAsync(string pipelineRtId, Guid executionId);

    /// <summary>
    ///     Returns a specific debug point.
    /// </summary>
    /// <param name="pipelineRtId">The pipeline runtime object ID.</param>
    /// <param name="executionId">The execution id.</param>
    /// <param name="nodeId">The node id.</param>
    Task<DebugPointDataDto> GetDebugPointAsync(string pipelineRtId, Guid executionId, string nodeId);

    // ── Triggers ──────────────────────────────────────────────────────────

    /// <summary>
    ///     Deploys triggers for the tenant.
    /// </summary>
    Task DeployTriggersAsync();

    /// <summary>
    ///     Undeploys triggers for the tenant.
    /// </summary>
    Task UndeployTriggersAsync();

    // ── Pools ─────────────────────────────────────────────────────────────

    /// <summary>
    ///     Returns a list of all pools for the tenant.
    /// </summary>
    Task<IReadOnlyList<PoolSummaryDto>> GetPoolsAsync();

    // ── Data Flows ────────────────────────────────────────────────────────

    /// <summary>
    ///     Deploys a data flow.
    /// </summary>
    /// <param name="dataFlowRtId">The data flow runtime object ID.</param>
    Task DeployDataFlowAsync(string dataFlowRtId);

    /// <summary>
    ///     Undeploys a data flow.
    /// </summary>
    /// <param name="dataFlowRtId">The data flow runtime object ID.</param>
    Task UndeployDataFlowAsync(string dataFlowRtId);

    /// <summary>
    ///     Gets the aggregated execution status of a data flow.
    /// </summary>
    /// <param name="dataFlowRtId">The data flow runtime object ID.</param>
    Task<DataFlowStatusDto> GetDataFlowStatusAsync(string dataFlowRtId);
}
