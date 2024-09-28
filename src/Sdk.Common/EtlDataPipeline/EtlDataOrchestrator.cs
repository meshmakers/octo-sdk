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
    public async Task<object?> ExecutePipelineAsync<TEtlContext>(PipelineConfigurationRoot pipelineConfigurationRoot,
        TEtlContext etlContext, IPipelineDebugger? pipelineDebugger = null, object? value = null)
        where TEtlContext : class, IEtlContext
    {
        var logger = pipelineDebugger?.Logger ?? _globalServiceProvider.GetRequiredService<IPipelineLogger>();

        // Create a new scope per execution;
        // we can't dispose the scope here, because some nodes will create their subpipeline which will outlive this scope.
        var scope = _globalServiceProvider.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var contextAccessor = serviceProvider.GetRequiredService<IEtlContextAccessor<TEtlContext>>();
        contextAccessor.EtlContextFactory = () => etlContext;
        
        DataContext dataContext = new(serviceProvider, logger, value, pipelineDebugger);
        pipelineDebugger?.BeginPipelineExecution();

        if (pipelineConfigurationRoot.Transformations == null)
        {
            dataContext.NodeContext.Warning("No transformations found in the pipeline configuration");
            return null;
        }

        // This is the last delegate in the sequence -> it will call the next node in the pipeline
        NodeDelegate nextDelegate = d =>
        {
            d.NodeContext.Complete(d);

            dataContext.Current = d.Current;
            return Task.CompletedTask;
        };
        var rootNodeContext = dataContext.NodeContext;

        try
        {
            rootNodeContext.Info("Executing pipeline");
            uint sequenceNumber = 0;
            foreach (var nodeConfiguration in pipelineConfigurationRoot.Transformations.Reverse())
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
                nextDelegate = async d =>
                {
                    d.NodeContext.Complete(d);

                    var nodeContext = d.RegisterChildNode(rootNodeContext, nodeQualifiedName!, sequenceNumber++,
                        nodeConfiguration);
                    nodeContext.Debug("Forward Executing");
                    await node!.ProcessObjectAsync(dataContext);
                    nodeContext.Debug("Reverse completed");
                };
            }

            await nextDelegate(dataContext);
            rootNodeContext.Info("Pipeline completed");
            rootNodeContext.Complete(dataContext);
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