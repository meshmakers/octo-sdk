using Meshmakers.Common.Shared.Services;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration.DependencyInjection;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration.Serializer;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Debugger;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Buffering;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Buffering.EdgeBuffer;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Extracts;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Loads;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Triggers;
using Meshmakers.Octo.Sdk.Common.Services;

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
        pipelineBuilder.RegisterNodeConfiguration<SelectByPathNodeConfiguration>();
        pipelineBuilder.RegisterNodeConfiguration<ForEachNodeConfiguration>();
        pipelineBuilder.RegisterNodeConfiguration<ForNodeConfiguration>();
        pipelineBuilder.RegisterNodeConfiguration<IfNodeConfiguration>();
        
        // Register extract nodes
        pipelineBuilder.RegisterNodeConfiguration<SetArrayOfPrimitiveValuesNodeConfiguration>();
        pipelineBuilder.RegisterNodeConfiguration<SetJsonNodeConfiguration>();
        pipelineBuilder.RegisterNodeConfiguration<SetPrimitiveValueNodeConfiguration>();
        
        // Register load nodes
        pipelineBuilder.RegisterNodeConfiguration<ToPipelineDataEventNodeConfiguration>();

        // Register transform nodes
        pipelineBuilder.RegisterNodeConfiguration<ConvertDataTypeNodeConfiguration>();
        pipelineBuilder.RegisterNodeConfiguration<FlattenNodeConfiguration>();
        pipelineBuilder.RegisterNodeConfiguration<LinearScalerNodeConfiguration>();
        pipelineBuilder.RegisterNodeConfiguration<LoggerNodeConfiguration>();
        pipelineBuilder.RegisterNodeConfiguration<MapNodeConfiguration>();
        pipelineBuilder.RegisterNodeConfiguration<PrintDebugNodeConfiguration>();
        pipelineBuilder.RegisterNodeConfiguration<ProjectNodeConfiguration>();
        
        // Register trigger nodes
        pipelineBuilder.RegisterNodeConfiguration<FromPipelineDataEventNodeConfiguration>();
        pipelineBuilder.RegisterNodeConfiguration<FromPollingNodeConfiguration>();
        
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
        services.AddTransient<ICompressionService, CompressionService>();
        services.AddTransient<IEtlDataOrchestrator, EtlDataOrchestrator>();

        services.AddSingleton<IEdgeDataBuffer, EdgeDataBuffer>();
        services.AddSingleton<ILiteDBFactory, LiteDbFileFactory>();
        services.AddSingleton<IContextCreatorService, DefaultContextCreatorService>();
        
        services.AddHostedService<BufferSchedulerHostedService>();
        services.AddSingleton<IBufferScheduler, BufferScheduler>();
        
        // EtlContext
        services.AddScoped(typeof(IEtlContextAccessor<>), typeof(EtlContextAccessor<>));
        
        // Register control nodes
        builder.RegisterNode<SelectByPathNode>();
        builder.RegisterNode<ForEachNode>();
        builder.RegisterNode<ForNode>();
        builder.RegisterNode<IfNode>();
        
        // Register extract nodes
        builder.RegisterNode<SetArrayOfPrimitiveValuesNode>();
        builder.RegisterNode<SetJsonNode>();
        builder.RegisterNode<SetPrimitiveValueNode>();
        builder.RegisterNode<BufferRetrievalNode>();

        // Register load nodes
        builder.RegisterNode<BufferNode>();
        builder.RegisterNode<ToPipelineDataEventNode>();

        // Register transform nodes
        builder.RegisterNode<ConvertDataTypeNode>();
        builder.RegisterNode<FlattenNode>();
        builder.RegisterNode<LinearScalerNode>();
        builder.RegisterNode<LoggerNode>();
        builder.RegisterNode<MapNode>();
        builder.RegisterNode<PrintDebugNode>();
        builder.RegisterNode<ProjectNode>();
        
        // Register trigger nodes
        builder.RegisterTriggerNode<FromPipelineDataEventNode>();
        builder.RegisterTriggerNode<FromPollingNode>();
        
        return builder;
    }
}