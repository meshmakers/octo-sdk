using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.Sdk.Common.Adapters;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration.Serializer;
using NLog;

namespace Meshmakers.Octo.Sdk.Common.Services;

/// <summary>
/// Adapter pipeline execution service
/// </summary>
/// <param name="etlDataOrchestrator"></param>
/// <param name="pipelineConfigurationSerializer"></param>
public class AdapterPipelineExecutionService(
    IEtlDataOrchestrator etlDataOrchestrator,
    IPipelineConfigurationSerializer pipelineConfigurationSerializer)
    : PipelineExecutionService(pipelineConfigurationSerializer)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <inheritdoc />
    public override async Task ExecutePipelineAsync(string tenantId, OctoObjectId pipelineRtId, ExecutePipelineOptions executePipelineOptions, object? value = null)
    {
        if (!PipelineExecutionItems.TryGetValue(CreateKey(tenantId, pipelineRtId), out var pipelineExecutionItem))
        {
            Logger.Error("Pipeline {Id} not found in tenant '{TenantId}'", pipelineRtId, tenantId);
            return;
        }

        try
        {
            Logger.Info("Execute pipeline {Id}: {Name}", pipelineExecutionItem.PipelineRtId, pipelineExecutionItem.PipelineName);
            var adapterEtlContext = new AdapterEtlContext(pipelineExecutionItem.TenantId, pipelineExecutionItem.PipelineRtId, 
                executePipelineOptions.TransactionStartedDateTime, executePipelineOptions.ExternalReceivedDateTime, pipelineExecutionItem.Dictionary);
            await etlDataOrchestrator.ExecutePipelineAsync<IAdapterEtlContext>(pipelineExecutionItem.ConfigurationRoot, adapterEtlContext);
        }
        catch (Exception e)
        {
            Logger.Error(e, "Error while executing pipeline {Id}: {Name}", pipelineExecutionItem.PipelineRtId, pipelineExecutionItem.PipelineName);
        }
    }
}