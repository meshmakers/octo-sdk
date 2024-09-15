using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.Sdk.Common.Adapters;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration.Serializer;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Debugger;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Meshmakers.Octo.Sdk.Common.Services;

/// <summary>
/// Adapter pipeline execution service
/// </summary>
/// <param name="logger">Logger</param>
/// <param name="serviceProvider">Service provider</param>
/// <param name="etlDataOrchestrator">Etl data orchestrator</param>
/// <param name="pipelineConfigurationSerializer">Pipeline configuration serializer</param>
public class AdapterPipelineExecutionService(
    ILogger<AdapterPipelineExecutionService> logger,
    IEtlDataOrchestrator etlDataOrchestrator,
    IServiceProvider serviceProvider,
    IPipelineConfigurationSerializer pipelineConfigurationSerializer)
    : PipelineExecutionService(pipelineConfigurationSerializer)
{
    /// <inheritdoc />
    public override Task<Guid> StartExecutePipelineAsync(string tenantId, RtEntityId pipelineRtEntityId,
        ExecutePipelineOptions executePipelineOptions, object? value = null)
    {
        if (!PipelineRegistrationsById.TryGetValue(CreateByIdKey(tenantId, pipelineRtEntityId),
                out var pipelineRegistration))
        {
            logger.LogError("[{TenantId}] Pipeline {Id} not found", pipelineRtEntityId, tenantId);
            throw PipelineExecutionException.PipelineNotFound(tenantId, pipelineRtEntityId);
        }
        
        var pipelineExecutionId = Guid.NewGuid();
        logger.LogDebug("[{TenantId}] Running pipeline for pipeline {PipelineRtEntityId} as run with execution id {PipelineExecutionId}", tenantId,
            pipelineRtEntityId, pipelineExecutionId);
        var adapterEtlContext = new AdapterEtlContext(pipelineRegistration.TenantId, 
            pipelineRegistration.DataPipelineRtId, pipelineExecutionId, pipelineRegistration.PipelineRtEntityId,
            executePipelineOptions.TransactionStartedDateTime, executePipelineOptions.ExternalReceivedDateTime,
            pipelineRegistration.Dictionary);
        
        IPipelineDebugger? debugger = null;
        if (pipelineRegistration.IsDebuggingEnabled)
        {
            logger.LogWarning("[{TenantId}] Debugging enabled for pipeline {PipelineRtEntityId} with execution id {PipelineExecutionId}", tenantId,
                pipelineRtEntityId, pipelineExecutionId);

            debugger = serviceProvider.GetRequiredService<IPipelineDebugger>();
            debugger.RegisterPipelineRtEntityId(pipelineRtEntityId, pipelineExecutionId);
        }
        
        DateTime startedDateTime = DateTime.UtcNow;
        var task = etlDataOrchestrator.ExecutePipelineAsync<IAdapterEtlContext>(pipelineRegistration.ConfigurationRoot,
            adapterEtlContext, debugger, value);
        pipelineRegistration.RegisterExecution(pipelineExecutionId, startedDateTime, task);

        return Task.FromResult(pipelineExecutionId);
    }

    /// <inheritdoc />
    public override async Task<object?> EndExecutePipelineAsync(string tenantId, RtEntityId pipelineRtEntityId, Guid pipelineExecutionId)
    {
        if (!PipelineRegistrationsById.TryGetValue(CreateByIdKey(tenantId, pipelineRtEntityId),
                out var pipelineRegistration))
        {
            logger.LogError("[{TenantId}] Pipeline {Id} not found", pipelineRtEntityId, tenantId);
            throw PipelineExecutionException.PipelineNotFound(tenantId, pipelineRtEntityId);
        }
        
        var result  = await pipelineRegistration.UnregisterExecutionAsync(pipelineExecutionId);
        logger.LogDebug("[{TenantId}] Pipeline finished for pipeline {PipelineRtEntityId}", tenantId,
            pipelineRtEntityId);
        return result;
    }
}