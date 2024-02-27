using Meshmakers.Octo.Sdk.Common.Adapters;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline;

/// <summary>
/// Implements an extract-transform-load data orchestrator
/// </summary>
public class EtlDataOrchestrator : IEtlDataOrchestrator
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
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
    public async Task<object?> ExecutePipelineAsync<TContext>(PipelineConfigurationRoot pipelineConfigurationRoot,
        TContext etlContext)
        where TContext : class, IEtlContext
    {
        if (pipelineConfigurationRoot.Transformations == null)
        {
            _logger.Warn("No transformations found in the pipeline configuration");
            return null;
        }


        // Create a new scope per execution
        using var scope = _globalServiceProvider.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        // configure the IEtlContextAccessor
        var contextAccessor = scope.ServiceProvider.GetRequiredService<IEtlContextAccessor>();
        contextAccessor.EtlContextFactory = () => etlContext;
        contextAccessor.AdapterEtlContextFactory = () => (IAdapterEtlContext)etlContext;

        DataContext dataContext = new(serviceProvider, serviceProvider);

        
        // This is the last delegate in the sequence -> it will call the next node in the pipeline
        NodeDelegate nextDelegate = d =>
        {
            _logger.Info("Pipeline execution completed");
            dataContext.Current = d.Current;
            return Task.CompletedTask;
        };

        try
        {
            _logger.Info("Starting pipeline execution");
            foreach (var nodeConfiguration in pipelineConfigurationRoot.Transformations.Reverse())
            {
                if (!_nodeLookupService.TryGetNodeConfigurationQualifiedName(nodeConfiguration.GetType(),
                        out var nodeQualifiedName))
                {
                    throw DataPipelineException.UnknownConfigurationType(nodeConfiguration.GetType());
                }

                if (!_nodeLookupService.TryCreateInstance(scope.ServiceProvider, nodeQualifiedName, nextDelegate, out var node))
                {
                    throw DataPipelineException.UnknownObjectPipelineNode(nodeQualifiedName);
                }

                // This is the next delegate in the sequence -> it will call the next node in the sequence            
                nextDelegate = async d =>
                {
                    var clone = d.Clone();
                    ((DataContext)clone).SetConfigurationNode(nodeConfiguration);
                    await node.ProcessObjectAsync(clone);
                };
            }

            await nextDelegate(dataContext);
        }
        catch (Exception e)
        {
            _logger.Error(e, "Error during pipeline execution");
            throw;
        }
        finally
        {
            // reset the IEtlContextAccessor
            contextAccessor.EtlContextFactory = null;
        }


        return dataContext.Current;
    }
}