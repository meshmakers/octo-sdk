using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration.DependencyInjection;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration.Serializer;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Debugger;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Buffering;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Buffering.EdgeBuffer;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Loads;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     Extension methods for adding Ck model compiler services to the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Adds Octo Mesh data pipeline serializer services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services"></param>
    public static IDataPipelineBuilder AddDataPipelineSerializer(this IServiceCollection services)
    {
        // Adding serializers
        services.AddSingleton<IPipelineConfigurationSerializer, YamlPipelineConfigurationSerializer>();
        services.AddSingleton<IJsonPipelineConfigurationSerializer, JsonPipelineConfigurationSerializer>();
        
        var pipelineBuilder = new DataPipelineBuilder(services);
        
        // Register control nodes
        pipelineBuilder.RegisterNodeConfiguration<SequenceNodeConfiguration>();
        pipelineBuilder.RegisterNodeConfiguration<SelectByPathNodeConfiguration>();
        pipelineBuilder.RegisterNodeConfiguration<SplitterNodeConfiguration>();

        // Register transform nodes
        pipelineBuilder.RegisterNodeConfiguration<ConvertDataTypeNodeConfiguration>();
        pipelineBuilder.RegisterNodeConfiguration<LinearScalerNodeConfiguration>();
        pipelineBuilder.RegisterNodeConfiguration<ProjectNodeConfiguration>();

        // Register load nodes
        pipelineBuilder.RegisterNodeConfiguration<DistributionEventHubNodeConfiguration>();
        
        return pipelineBuilder;
    }
    
    /// <summary>
    /// Adds ETL pipeline services
    /// </summary>
    /// <returns></returns>
    public static IDataPipelineBuilder AddDataPipeline(this IServiceCollection services)
    {
        var builder = services.AddDataPipelineSerializer();
        
        // Add orchestrator
        services.AddTransient<IPipelineLogger, DefaultPipelineLogger>();
        services.AddTransient<IPipelineDebugSerializer, PipelineDebugSerializer>();
        services.AddTransient<IEtlDataOrchestrator, EtlDataOrchestrator>();

        services.AddSingleton<IEdgeDataBuffer, EdgeDataBuffer>();
        services.AddSingleton<ILiteDBFactory, LiteDbFileFactory>();

        // EtlContext
        services.AddScoped(typeof(IEtlContextAccessor<>), typeof(EtlContextAccessor<>));

        // Register control nodes
        builder.RegisterNode<SequenceNode>();
        builder.RegisterNode<SelectByPathNode>();
        builder.RegisterNode<SplitterNode>();

        // Register transform nodes
        builder.RegisterNode<ConvertDataTypeNode>();
        builder.RegisterNode<LinearScalerNode>();
        builder.RegisterNode<ProjectNode>();

        // Register buffer
        builder.RegisterNode<BufferNode>();
        
        // Register load nodes
        builder.RegisterNode<DistributionEventHubNode>();
        
        return builder;
    }
}