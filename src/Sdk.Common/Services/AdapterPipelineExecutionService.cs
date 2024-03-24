using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.Sdk.Common.Adapters;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration.Serializer;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Debugger;
using Microsoft.Extensions.Logging;

namespace Meshmakers.Octo.Sdk.Common.Services;

/// <summary>
/// Adapter pipeline execution service
/// </summary>
/// <param name="loggerFactory">Factory for creating logger instances</param>
/// <param name="logger">Logger</param>
/// <param name="pipelineDebugSerializer">Pipeline debug information serializer</param>
/// <param name="etlDataOrchestrator">Etl data orchestrator</param>
/// <param name="pipelineConfigurationSerializer">Pipeline configuration serializer</param>
public class AdapterPipelineExecutionService(
    ILoggerFactory loggerFactory,
    ILogger<AdapterPipelineExecutionService> logger,
    IEtlDataOrchestrator etlDataOrchestrator,
    IPipelineDebugSerializer pipelineDebugSerializer,
    IPipelineConfigurationSerializer pipelineConfigurationSerializer)
    : PipelineExecutionService(pipelineConfigurationSerializer)
{
    /// <inheritdoc />
    public override async Task ExecutePipelineAsync(string tenantId, OctoObjectId pipelineRtId, ExecutePipelineOptions executePipelineOptions, object? value = null)
    {
        if (!PipelineExecutionItems.TryGetValue(CreateKey(tenantId, pipelineRtId), out var pipelineExecutionItem))
        {
            logger.LogError("Pipeline {Id} not found in tenant '{TenantId}'", pipelineRtId, tenantId);
            return;
        }

        try
        {
            logger.LogInformation("Execute pipeline {Id}", pipelineExecutionItem.PipelineRtId);
            var adapterEtlContext = new AdapterEtlContext(pipelineExecutionItem.TenantId, pipelineExecutionItem.PipelineRtId, 
                executePipelineOptions.TransactionStartedDateTime, executePipelineOptions.ExternalReceivedDateTime, pipelineExecutionItem.Dictionary);

            IPipelineDebugger? debugger = null;
            if (pipelineExecutionItem.IsDebuggingEnabled)
            {
                debugger = new DefaultPipelineDebugger(loggerFactory);
            }

            await etlDataOrchestrator.ExecutePipelineAsync<IAdapterEtlContext>(pipelineExecutionItem.ConfigurationRoot, adapterEtlContext, debugger);
            
            if (debugger != null)
            {
                var debugInfo = await pipelineDebugSerializer.SerializeAsync(debugger.GetDebugInformation());
                await executePipelineOptions.SendDebugInfoFunc(pipelineExecutionItem.PipelineRtId, debugInfo);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while executing pipeline {Id}", pipelineExecutionItem.PipelineRtId);
        }
    }
}