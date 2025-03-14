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
    public async Task<object?> ExecutePipelineAsync<TEtlContext>(NodeDefinitionRoot nodeDefinitionRoot,
        TEtlContext etlContext, IPipelineDebugger? pipelineDebugger = null, object? value = null)
        where TEtlContext : class, IEtlContext
    {
        var logger = pipelineDebugger?.Logger ?? _globalServiceProvider.GetRequiredService<IPipelineLogger>();

        // Create a new scope per execution;
        // we can't dispose the scope here, because some nodes will create their sub pipeline, which will outlive this scope.
        var scope = _globalServiceProvider.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var contextAccessor = serviceProvider.GetRequiredService<IEtlContextAccessor<TEtlContext>>();
        contextAccessor.EtlContextFactory = () => etlContext;
        
        DataContext dataContext = new(value);
        var rootNodeContext = NodeContext.CreateRootNodeContext(serviceProvider, logger, dataContext, pipelineDebugger);

        pipelineDebugger?.BeginPipelineExecution();

        if (nodeDefinitionRoot.Transformations == null)
        {
            rootNodeContext.Warning("No transformations found in the pipeline configuration");
            return null;
        }

        // This is the last delegate in the sequence -> it will call the next node in the pipeline
        NodeDelegate nextDelegate = (ds, nc) =>
        {
            nc.Unregister(ds);

            dataContext.Current = ds.Current;
            return Task.CompletedTask;
        };

        try
        {
            rootNodeContext.Info("Executing pipeline");

            uint sequenceNumber = 0;
            foreach (var nodeConfiguration in nodeDefinitionRoot.Transformations.Reverse())
            {
                if (!_nodeLookupService.TryGetNodeConfigurationQualifiedName(nodeConfiguration.GetType(),
                        out var nodeQualifiedName))
                {
                    throw DataPipelineException.UnknownConfigurationType(nodeConfiguration.GetType());
                }

                if (!_nodeLookupService.TryCreateInstance(serviceProvider, nodeQualifiedName!, nextDelegate,
                        out var node))
                {
                    throw DataPipelineException.UnknownObjectPipelineNode(nodeQualifiedName!);
                }


                // This is the next delegate in the sequence -> it will call the next node in the sequence            
                nextDelegate = async (ds, nc) =>
                {
                    nc.Unregister(ds);

                    var nodeContext = rootNodeContext.RegisterChildNode(nodeQualifiedName!, sequenceNumber++,
                        nodeConfiguration, ds);
                    nodeContext.Debug("Forward Executing");
                    await node!.ProcessObjectAsync(ds, nodeContext);
                    nodeContext.Debug("Reverse completed");
                };
            }

            await nextDelegate(dataContext, rootNodeContext);
            rootNodeContext.Info("Pipeline completed");
            rootNodeContext.Unregister(dataContext);
        }
        catch (Exception e)
        {
            rootNodeContext.Error(e, "Error during pipeline execution");
            throw;
        }
        finally
        {
            try
            {
                // end debugging
                if (pipelineDebugger != null)
                {
                    await pipelineDebugger.EndPipelineExecutionAsync();
                }
            }
            catch (Exception e)
            {
                rootNodeContext.Error(e, "Error during sending debug information");
                throw;
            }
        }

        return dataContext.Current;
    }
}