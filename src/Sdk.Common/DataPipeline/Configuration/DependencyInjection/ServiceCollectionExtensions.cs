using Meshmakers.Octo.Sdk.Common.DataPipeline.Configuration;

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
        services.AddTransient<IPipelineConfigurationSerializer, YamlPipelineConfigurationSerializer>();

        // Add rule engine

        // Implementation of bulk operations
    }
}