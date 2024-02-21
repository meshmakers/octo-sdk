using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline;

/// <summary>
/// Implements an extract-transform-load data orchestrator
/// </summary>
public class EtlDataOrchestrator : IEtlDataOrchestrator
{
    private readonly IServiceProvider _globalServiceProvider;
    private readonly INodeLookupService _nodeLookupService;

    /// <summary>
    /// Creates a new instance of <see cref="EtlDataOrchestrator"/>
    /// </summary>
    /// <param name="globalServiceProvider"></param>
    /// <param name="nodeLookupService"></param>
    public EtlDataOrchestrator(IServiceProvider globalServiceProvider, INodeLookupService nodeLookupService)
    {
        _globalServiceProvider = globalServiceProvider;
        _nodeLookupService = nodeLookupService;
    }

    /// <inheritdoc />
    public async Task ExecutePipelineAsync<TContext>(PipelineConfigurationRoot pipelineConfigurationRoot, TContext etlContext)
        where TContext : class, IEtlContext
    {
        ServiceCollection pipelineServices = new();
        pipelineServices.AddSingleton<IEtlContext>(_ => etlContext);
        pipelineServices.AddSingleton<TContext>(_ => etlContext);

        await using var pipelineServiceProvider = pipelineServices.BuildServiceProvider();

        // Stage: Extract
        var extractDataContext = await ExecuteExtractStage(pipelineServiceProvider, pipelineConfigurationRoot);

        // We can't continue if there is no source data
        if (extractDataContext.Source == null)
        {
            return;
        }

        // Stage: Transform
        var transformDataContext = await ExecuteTransformStage(pipelineServiceProvider, extractDataContext, pipelineConfigurationRoot);

        // Stage: Load
        await ExecuteLoadStage(pipelineServiceProvider, transformDataContext, pipelineConfigurationRoot);
    }

    private async Task<ExtractDataContext> ExecuteExtractStage(IServiceProvider pipelineServiceProvider,
        PipelineConfigurationRoot pipelineConfigurationRoot)
    {
        if (pipelineConfigurationRoot.Extracts == null)
        {
            throw DataPipelineException.NoExtractsConfigured();
        }

        ExtractDataContext extractDataContext = new(_globalServiceProvider, pipelineServiceProvider);
        foreach (var extractConfigurationNode in pipelineConfigurationRoot.Extracts)
        {
            if (!_nodeLookupService.TryGetNodeQualifiedName(extractConfigurationNode.GetType(), out var nodeQualifiedName))
            {
                throw DataPipelineException.UnknownConfigurationType(extractConfigurationNode.GetType());
            }

            if (!_nodeLookupService.TryGetExtractPipelineNode(nodeQualifiedName, out var node))
            {
                throw DataPipelineException.UnknownObjectPipelineNode(nodeQualifiedName);
            }

            extractDataContext.SetConfigurationNode(extractConfigurationNode);
            await node.ProcessObjectAsync(extractDataContext);
        }

        return extractDataContext;
    }

    private async Task<TransformDataContext> ExecuteTransformStage(IServiceProvider pipelineServiceProvider,
        ExtractDataContext extractDataContext, PipelineConfigurationRoot pipelineConfigurationRoot)
    {
        TransformDataContext transformDataContext = new(_globalServiceProvider, pipelineServiceProvider,
            extractDataContext.Source == null ? null : JObject.FromObject(extractDataContext.Source));
        if (pipelineConfigurationRoot.Transformations != null)
        {
            foreach (var configurationNode in pipelineConfigurationRoot.Transformations)
            {
                if (!_nodeLookupService.TryGetNodeQualifiedName(configurationNode.GetType(), out var nodeQualifiedName))
                {
                    throw DataPipelineException.UnknownConfigurationType(configurationNode.GetType());
                }

                if (!_nodeLookupService.TryGetTransformPipelineNode(nodeQualifiedName, out var node))
                {
                    throw DataPipelineException.UnknownObjectPipelineNode(nodeQualifiedName);
                }

                transformDataContext.SetConfigurationNode(configurationNode);
                await node.ProcessObjectAsync(transformDataContext);
            }
        }

        return transformDataContext;
    }

    private async Task ExecuteLoadStage(IServiceProvider pipelineServiceProvider, TransformDataContext transformDataContext,
        PipelineConfigurationRoot pipelineConfigurationRoot)
    {
        LoadDataContext loadDataContext = new(_globalServiceProvider, pipelineServiceProvider, transformDataContext.Target);
        if (pipelineConfigurationRoot.Loads != null)
        {
            foreach (var configurationNode in pipelineConfigurationRoot.Loads)
            {
                if (!_nodeLookupService.TryGetNodeQualifiedName(configurationNode.GetType(), out var nodeQualifiedName))
                {
                    throw DataPipelineException.UnknownConfigurationType(configurationNode.GetType());
                }

                if (!_nodeLookupService.TryGetLoadPipelineNode(nodeQualifiedName, out var node))
                {
                    throw DataPipelineException.UnknownObjectPipelineNode(nodeQualifiedName);
                }

                loadDataContext.SetConfigurationNode(configurationNode);
                await node.ProcessObjectAsync(loadDataContext);
            }
        }
    }
}