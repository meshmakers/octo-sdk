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
    Task SendDeploymentResultAsync(RtEntityId adapterRtEntityId, DeploymentResult deploymentResult);
}