using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Microsoft.Extensions.Logging;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline;

/// <summary>
/// Base interface for data context
/// </summary>
public interface IDataContext
{
    /// <summary>
    /// Provider for services that are global for the whole application
    /// </summary>
    IServiceProvider GlobalServiceProvider { get; }
    
    /// <summary>
    /// Provider for services that are specific for the current pipeline
    /// </summary>
    IServiceProvider PipelineServiceProvider { get; }
    
    /// <summary>
    /// Provider for logging
    /// </summary>
    ILogger Logger { get; }

    /// <summary>
    /// Get configuration for the current node
    /// </summary>
    /// <typeparam name="T">Generic type of configuration</typeparam>
    /// <returns></returns>
    T GetNodeConfiguration<T>() where T : NodeConfiguration;
}