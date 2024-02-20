using Meshmakers.Octo.Sdk.Common.DataPipeline.Configuration;
using Microsoft.Extensions.Logging;

namespace Meshmakers.Octo.Sdk.Common.DataPipeline;

/// <summary>
/// Base interface for data context
/// </summary>
public interface IDataContext
{
    /// <summary>
    /// Provider for services
    /// </summary>
    IServiceProvider ServiceProvider { get; }
    
    /// <summary>
    /// Provider for logging
    /// </summary>
    ILogger Logger { get; }

    /// <summary>
    /// Get configuration for the current node
    /// </summary>
    /// <typeparam name="T">Generic type of configuration</typeparam>
    /// <returns></returns>
    T GetNodeConfiguration<T>() where T : ConfigurationNode;
}