using Meshmakers.Octo.Sdk.Common.Adapters;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration.DependencyInjection;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration.Serializer;
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
    ///     Adds Octo Mesh data pipeline services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IDataPipelineBuilder AddDataPipeline(
        this IServiceCollection services)
    {
        // Dependencies

        // Adding serializers
        services.AddSingleton<IPipelineConfigurationSerializer, YamlPipelineConfigurationSerializer>();
        services.AddSingleton<IJsonPipelineConfigurationSerializer, JsonPipelineConfigurationSerializer>();
        
        // Add orchestrator
        services.AddTransient<IEtlDataOrchestrator, EtlDataOrchestrator>();

        // EtlContext
        
        services.AddScoped(typeof(IEtlContextAccessor<>), typeof(EtlContextAccessor<>));

        var pipelineBuilder = new DataPipelineBuilder(services);
        
        // Register control nodes
        pipelineBuilder.RegisterNode<SequenceNode>();
        pipelineBuilder.RegisterNode<SelectByPathNode>();
        pipelineBuilder.RegisterNode<SplitterNode>();
        
        // Register transform nodes
        pipelineBuilder.RegisterNode<ConvertDataTypeNode>();
        pipelineBuilder.RegisterNode<LinearScalerNode>();
        pipelineBuilder.RegisterNode<ProjectNode>();

        // Register load nodes
        pipelineBuilder.RegisterNode<DistributionEventHubNode>();

        return pipelineBuilder;
    }
}