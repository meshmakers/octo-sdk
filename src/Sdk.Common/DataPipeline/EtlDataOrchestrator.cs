using Meshmakers.Octo.Sdk.Common.DataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.DataPipeline.Nodes;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.DataPipeline;

/// <summary>
/// Implements an extract-transform-load data orchestrator
/// </summary>
public class EtlDataOrchestrator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly PipelineConfigurationRoot _pipelineConfigurationRoot;
    private readonly INodeLookupService _nodeLookupService;

    /// <summary>
    /// Creates a new instance of <see cref="EtlDataOrchestrator"/>
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="pipelineConfigurationRoot">Configuration of the data pipeline to run</param>
    /// <param name="nodeLookupService"></param>
    public EtlDataOrchestrator(IServiceProvider serviceProvider, PipelineConfigurationRoot pipelineConfigurationRoot,
        INodeLookupService nodeLookupService)
    {
        _serviceProvider = serviceProvider;
        _pipelineConfigurationRoot = pipelineConfigurationRoot;
        _nodeLookupService = nodeLookupService;
    }
    
    /// <summary>
    /// Executes the pipeline
    /// </summary>
    public async Task ExecutePipelineAsync()
    {
        // Stage: Extract
        var extractDataContext = await ExecuteExtractStage();

        // We can't continue if there is no source data
        if (extractDataContext.Source == null)
        {
            return;
        }

        // Stage: Transform
        var transformDataContext = await ExecuteTransformStage(extractDataContext);

        // Stage: Load
        await ExecuteLoadStage(transformDataContext);
    }

    private async Task<ExtractDataContext> ExecuteExtractStage()
    {
        if (_pipelineConfigurationRoot.Extracts == null)
        {
            throw DataPipelineException.NoExtractsConfigured();
        }
        
        ExtractDataContext extractDataContext = new(_serviceProvider);
        foreach (var extractConfigurationNode in _pipelineConfigurationRoot.Extracts)
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

    private async Task<TransformDataContext> ExecuteTransformStage(ExtractDataContext extractDataContext)
    {
        TransformDataContext transformDataContext = new(_serviceProvider,  extractDataContext.Source == null ? null : JObject.FromObject(extractDataContext.Source));
        if (_pipelineConfigurationRoot.Transforms != null)
        {
            foreach (var configurationNode in _pipelineConfigurationRoot.Transforms)
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

    private async Task ExecuteLoadStage(TransformDataContext transformDataContext)
    {
        LoadDataContext loadDataContext = new(_serviceProvider, transformDataContext.Target);
        if (_pipelineConfigurationRoot.Loads != null)
        {
            foreach (var configurationNode in _pipelineConfigurationRoot.Loads)
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