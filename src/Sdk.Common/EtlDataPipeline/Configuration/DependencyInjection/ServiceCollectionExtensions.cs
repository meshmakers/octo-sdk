using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration.Serializer;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
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
    public static void AddDataPipeline(
        this IServiceCollection services)
    {
        // Dependencies

        // Adding serializers
        services.AddSingleton<IPipelineConfigurationSerializer, YamlPipelineConfigurationSerializer>();
        services.AddSingleton<INodeLookupService, NodeLookupService>();

        // Add nodes of extract stage
        
        // Add nodes of transform stage
        services.AddTransient<ITransformPipelineNode, ByPathTransformNode>();

        // Add nodes of load stage
        
        // Add signal processing
        services.AddTransient<ITransformPipelineNode, LinearScalerNode>();
        
    }
}