using Meshmakers.Octo.Sdk.Common.DataPipeline;
using Meshmakers.Octo.Sdk.Common.DataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.DataPipeline.Nodes;
using Meshmakers.Octo.Sdk.Common.DataPipeline.Nodes.Transforms;

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
        services.AddTransient<ITransformPipelineNode, TransformByPathTransformNode>();

        // Add nodes of load stage
        
        // Add signal processing
        services.AddTransient<ITransformPipelineNode, LinearScalerNode>();
        
    }
}