using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts;

namespace Meshmakers.Octo.Sdk.Common.Services;

/// <summary>
/// Interface for the pipeline execution service
/// </summary>
public interface IPipelineExecutionService
{
    /// <summary>
    /// Register a pipeline configuration
    /// </summary>
    /// <param name="tenantId">TenantId of pipeline</param>
    /// <param name="pipelineConfiguration">Pipeline configuration with transformation list</param>
    Task RegisterPipeline(string tenantId, PipelineConfigurationDto pipelineConfiguration);
    
    /// <summary>
    /// Unregister a pipeline configuration
    /// </summary>
    /// <param name="tenantId">TenantId of pipeline</param>
    /// <param name="pipelineRtEntityId">Pipeline runtime id</param>
    void UnregisterPipeline(string tenantId, RtEntityId pipelineRtEntityId);
    
    /// <summary>
    /// Updates a pipeline configuration
    /// </summary>
    /// <param name="tenantId">TenantId of pipeline</param>
    /// <param name="pipelineConfiguration">Pipeline configuration with transformation list</param>
    void UpdatePipeline(string tenantId, PipelineConfigurationDto pipelineConfiguration);
    
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

    /// <summary>
    /// Executes all registered pipelines
    /// </summary>
    /// <param name="executePipelineOptions">Options for executing the pipeline</param>
    /// <returns></returns>
    Task ExecuteAllPipelinesAsync(ExecutePipelineOptions executePipelineOptions);

    /// <summary>
    /// Executes a pipeline
    /// </summary>
    /// <param name="pipelineRtEntityId"></param>
    /// <param name="executePipelineOptions">Options for executing the pipeline</param>
    /// <param name="tenantId"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    Task ExecutePipelineAsync(string tenantId, RtEntityId pipelineRtEntityId, ExecutePipelineOptions executePipelineOptions, object? value = null);
}