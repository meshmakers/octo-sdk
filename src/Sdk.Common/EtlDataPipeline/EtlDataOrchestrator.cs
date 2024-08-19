using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Debugger;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Microsoft.Extensions.DependencyInjection;

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
    public async Task<object?> ExecutePipelineAsync<TContext>(PipelineConfigurationRoot pipelineConfigurationRoot,
        TContext etlContext, IPipelineDebugger? pipelineDebugger = null, object? value = null)
        where TContext : class, IEtlContext
    {
        var logger = pipelineDebugger?.Logger ?? _globalServiceProvider.GetRequiredService<IPipelineLogger>();
        
        // Create a new scope per execution;
        // we can't dispose the scope here, because some nodes will create their subpipeline which will outlive this scope.
        var scope = _globalServiceProvider.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        var contextAccessor = SetContextAccessor(etlContext, scope);

        DataContext dataContext = new(serviceProvider, logger, value, pipelineDebugger);
        pipelineDebugger?.BeginPipelineExecution();
        
        if (pipelineConfigurationRoot.Transformations == null)
        {
            logger.Warning(dataContext.NodeStack.Peek(), "No transformations found in the pipeline configuration");
            return null;
        }

        // This is the last delegate in the sequence -> it will call the next node in the pipeline
        NodeDelegate nextDelegate = d =>
        {
            dataContext.Current = d.Current;
            return Task.CompletedTask;
        };

        try
        {
            logger.Info(dataContext.NodeStack.Peek(), "Executing pipeline");
            foreach (var nodeConfiguration in pipelineConfigurationRoot.Transformations.Reverse())
            {
                if (!_nodeLookupService.TryGetNodeConfigurationQualifiedName(nodeConfiguration.GetType(),
                        out var nodeQualifiedName))
                {
                    throw DataPipelineException.UnknownConfigurationType(nodeConfiguration.GetType());
                }

                if (!_nodeLookupService.TryCreateInstance(scope.ServiceProvider, nodeQualifiedName!, nextDelegate,
                        out var node))
                {
                    throw DataPipelineException.UnknownObjectPipelineNode(nodeQualifiedName!);
                }

                // This is the next delegate in the sequence -> it will call the next node in the sequence            
                nextDelegate = async d =>
                {
                    var childNodePath = dataContext.NodeStack.Peek().Append(nodeQualifiedName!, nodeConfiguration.Description);
                    
                    var clone = new DataContext(d, childNodePath, nodeConfiguration);
                    clone.Logger.Debug(childNodePath, "Forward Executing");
                    await node!.ProcessObjectAsync(clone);
                    clone.Logger.Debug(childNodePath, "Reverse completed");
                    clone.PopNode();
                };
            }

            await nextDelegate(dataContext);
            dataContext.Logger.Debug(dataContext.NodeStack.Peek(), $"Pipeline completed");
        }
        catch (Exception e)
        {
            logger.Error(dataContext.NodeStack.Peek(), e, "Error during pipeline execution");
            throw;
        }
        finally
        {
            // contextAccessor.EtlContextFactory = null;
            //it is not required to reset the etl context factory due to the scoped nature of the service provider
            
            // end debugging
            if (pipelineDebugger != null)
            {
                await pipelineDebugger.EndPipelineExecutionAsync();
            }
        }

        return dataContext.Current;
    }

    private IEtlContextAccessor<TContext> SetContextAccessor<TContext>(TContext etlContext, IServiceScope scope)
        where TContext : class, IEtlContext
    {
        // configure the IEtlContextAccessor
        var contextAccessor = scope.ServiceProvider.GetRequiredService<IEtlContextAccessor<TContext>>();
        contextAccessor.EtlContextFactory = () => etlContext;
        
        var defaultContextAccessor = scope.ServiceProvider.GetRequiredService<IEtlContextAccessor<IEtlContext>>();
        defaultContextAccessor.EtlContextFactory = () => etlContext;
        
        return contextAccessor;
    }
}