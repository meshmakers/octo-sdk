using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts;
using System.Diagnostics.CodeAnalysis;
using Meshmakers.Octo.Communication.Contracts.Hubs;

namespace Meshmakers.Octo.Sdk.Common.Services;

/// <summary>
/// Interface for the pipeline registry
/// </summary>
public interface IPipelineRegistryService
{
    /// <summary>
    /// Register a pipeline configuration
    /// </summary>
    /// <param name="tenantId">TenantId of pipeline</param>
    /// <param name="pipelineConfiguration">Pipeline configuration with transformation list</param>
    Task RegisterPipelineAsync(string tenantId, PipelineConfigurationDto pipelineConfiguration);
    
    /// <summary>
    /// Registers multiple pipeline configurations
    /// </summary>
    /// <param name="tenantId">TenantId of the pipeline</param>
    /// <param name="pipelineConfigurations">List of pipeline configurations</param>
    /// <param name="deploymentErrorMessages">Error messages that occurred during the register operation</param>
    /// <returns></returns>
    Task<bool> RegisterPipelinesAsync(string tenantId, IEnumerable<PipelineConfigurationDto> pipelineConfigurations,
        List<DeploymentUpdateErrorMessageDto> deploymentErrorMessages);
    
    /// <summary>
    /// Unregister a pipeline configuration
    /// </summary>
    /// <param name="tenantId">TenantId of pipeline</param>
    /// <param name="pipelineRtEntityId">Pipeline runtime id</param>
    void UnregisterPipeline(string tenantId, RtEntityId pipelineRtEntityId);
    
    /// <summary>
    /// Unregister all pipelines
    /// </summary>
    void UnregisterAllPipelines(string tenantId);
    
    /// <summary>
    /// Returns if a pipeline is registered
    /// </summary>
    /// <param name="tenantId">TenantId of pipeline</param>
    /// <param name="pipelineRtEntityId">Pipeline runtime id</param>
    /// <returns></returns>
    bool IsRegistered(string tenantId, RtEntityId pipelineRtEntityId);
    
#if !NETSTANDARD2_0    
    /// <summary>
    /// Returns the pipeline registration if it exists 
    /// </summary>
    /// <param name="tenantId">TenantId of pipeline</param>
    /// <param name="pipelineRtEntityId">Pipeline runtime id</param>
    /// <param name="pipelineRegistration">Pipeline registration</param>
    /// <returns>The pipeline registration if it exists</returns>
    bool TryGetPipelineRegistration(string tenantId, RtEntityId pipelineRtEntityId, [NotNullWhen(true)] out PipelineRegistration? pipelineRegistration);
#else    
    /// <summary>
    /// Returns the pipeline registration if it exists 
    /// </summary>
    /// <param name="tenantId">TenantId of pipeline</param>
    /// <param name="pipelineRtEntityId">Pipeline runtime id</param>
    /// <param name="pipelineRegistration">Pipeline registration</param>
    /// <returns>The pipeline registration if it exists</returns>
    bool TryGetPipelineRegistration(string tenantId, RtEntityId pipelineRtEntityId, out PipelineRegistration? pipelineRegistration);
#endif
    
    /// <summary>
    /// Starts the trigger extract nodes
    /// </summary>
    /// <param name="tenantId">TenantId of pipeline</param>
    /// <returns></returns>
    Task StartTriggerPipelineNodesAsync(string tenantId);
    
    /// <summary>
    /// Stop the trigger extract nodes
    /// </summary>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    Task StopTriggerPipelineNodesAsync(string tenantId);
}