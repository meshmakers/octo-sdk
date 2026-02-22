using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Meshmakers.Octo.Communication.Contracts.Hubs;

/// <summary>
///     Interface of the adapter hub that is responsible for registering and unregistering adapters and managing their state.
/// </summary>
public interface IAdapterHub
{
    /// <summary>
    ///     Registers an adapter at the communication controller
    /// </summary>
    /// <param name="adapterRtEntityId">Object identifier of the adapter</param>
    /// <returns></returns>
    Task<AdapterConfigurationDto> RegisterAdapterAsync(RtEntityId adapterRtEntityId);

    /// <summary>
    ///     Registers an adapter at the communication controller with node descriptors
    /// </summary>
    /// <param name="adapterRtEntityId">Object identifier of the adapter</param>
    /// <param name="nodeDescriptors">Descriptors of pipeline nodes provided by this adapter</param>
    /// <returns></returns>
    Task<AdapterConfigurationDto> RegisterAdapterWithNodesAsync(RtEntityId adapterRtEntityId,
        IReadOnlyList<NodeDescriptorDto> nodeDescriptors);

    /// <summary>
    ///     Registers an adapter at the communication controller with node descriptors and a pipeline schema
    /// </summary>
    /// <param name="adapterRtEntityId">Object identifier of the adapter</param>
    /// <param name="nodeDescriptors">Descriptors of pipeline nodes provided by this adapter</param>
    /// <param name="pipelineSchemaJson">Composite JSON Schema for the full pipeline definition</param>
    /// <returns></returns>
    Task<AdapterConfigurationDto> RegisterAdapterWithSchemaAsync(RtEntityId adapterRtEntityId,
        IReadOnlyList<NodeDescriptorDto> nodeDescriptors, string pipelineSchemaJson);

    /// <summary>
    ///     Unregisters an adapter from the communication controller
    /// </summary>
    /// <param name="adapterRtEntityId">Object identifier of the adapter</param>
    /// <returns></returns>
    Task UnRegisterAdapterAsync(RtEntityId adapterRtEntityId);

    /// <summary>
    ///     Sends debug data to the communication controller
    /// </summary>
    /// <param name="pipelineRtEntityId">Object identifier of the pipeline</param>
    /// <param name="pipelineExecutionId">Guid that identifies the pipeline execution instance</param>
    /// <param name="debugPoint">Debug information of a node execution</param>
    /// <returns></returns>
    Task SendDebugDataAsync(RtEntityId pipelineRtEntityId, Guid pipelineExecutionId, DebugPointDto debugPoint);
    
    /// <summary>
    /// Updates the server about the result of an adapter configuration update.
    /// </summary>
    /// <param name="adapterRtEntityId">Object identifier of the adapter</param>
    /// <param name="deploymentResult">The result of the deployment</param>
    /// <returns></returns>
    Task SendDeploymentUpdateResultAsync(RtEntityId adapterRtEntityId, DeploymentResult deploymentResult);

    /// <summary>
    /// Reports the start of a pipeline execution to the communication controller.
    /// </summary>
    /// <param name="startDto">Details about the execution start</param>
    /// <returns></returns>
    Task ReportExecutionStartAsync(PipelineExecutionStartDto startDto);

    /// <summary>
    /// Reports the end of a pipeline execution to the communication controller.
    /// </summary>
    /// <param name="endDto">Details about the execution end including status and duration</param>
    /// <returns></returns>
    Task ReportExecutionEndAsync(PipelineExecutionEndDto endDto);

    /// <summary>
    /// Reports the final result of an execution that was previously marked as interrupted.
    /// Called after adapter reconnects to update the final status of interrupted executions.
    /// </summary>
    /// <param name="endDto">Details about the execution end</param>
    /// <returns></returns>
    Task ReportInterruptedExecutionResultAsync(PipelineExecutionEndDto endDto);

    /// <summary>
    /// Gets the list of execution IDs that were marked as interrupted when this adapter disconnected.
    /// Called after reconnection to determine which executions need their final status reported.
    /// </summary>
    /// <returns>List of execution IDs that are in interrupted state</returns>
    Task<IReadOnlyList<string>> GetInterruptedExecutionIdsAsync();
}