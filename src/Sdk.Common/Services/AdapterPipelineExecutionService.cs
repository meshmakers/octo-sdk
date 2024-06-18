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
    public override async Task ExecutePipelineAsync(string tenantId, RtEntityId pipelineRtEntityId,
        ExecutePipelineOptions executePipelineOptions, object? value = null)
    {
        if (!PipelineExecutionItemsById.TryGetValue(CreateByIdKey(tenantId, pipelineRtEntityId),
                out var pipelineExecutionItem))
        {
            logger.LogError("Pipeline {Id} not found in tenant '{TenantId}'", pipelineRtEntityId, tenantId);
            return;
        }

        try
        {
            logger.LogInformation("Execute pipeline {Id}", pipelineExecutionItem.PipelineRtEntityId);
            var adapterEtlContext = new AdapterEtlContext(pipelineExecutionItem.TenantId,
                pipelineExecutionItem.DataPipelineRtId, pipelineExecutionItem.PipelineRtEntityId,
                executePipelineOptions.TransactionStartedDateTime, executePipelineOptions.ExternalReceivedDateTime,
                pipelineExecutionItem.Dictionary);

            IPipelineDebugger? debugger = null;
            if (pipelineExecutionItem.IsDebuggingEnabled)
            {
                debugger = serviceProvider.GetRequiredService<IPipelineDebugger>();
                debugger.RegisterPipelineRtEntityId(pipelineRtEntityId);
            }

            await etlDataOrchestrator.ExecutePipelineAsync<IAdapterEtlContext>(pipelineExecutionItem.ConfigurationRoot,
                adapterEtlContext, debugger);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while executing pipeline {Id}", pipelineExecutionItem.PipelineRtEntityId);
        }
    }
}