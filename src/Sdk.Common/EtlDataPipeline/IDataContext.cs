using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

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
    T GetNodeConfiguration<T>() where T : INodeConfiguration;
    
    /// <summary>
    /// The current pipeline object. This is the object that is being processed by the pipeline in the transform stage.
    /// </summary>
    public JToken? Current { get; set; }
    
    /// <summary>
    /// Get the value as a specific type
    /// </summary>
    /// <param name="path">Path to the value</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    T? GetCurrentValueByPath<T>(string path);
    
    /// <summary>
    /// Get the value as a specific type
    /// </summary>
    /// <param name="propertyName">Property name</param>
    /// <typeparam name="T"></typeparam>
    T? GetCurrentValueByName<T>(string? propertyName);
    
    /// <summary>
    /// Get the value as a specific type
    /// </summary>
    /// <param name="propertyName">Property name</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    IEnumerable<T?>? GetCurrentValuesByName<T>(string? propertyName);

    /// <summary>
    /// Set the value as a specific type
    /// </summary>
    /// <param name="propertyName">Property name</param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    void SetCurrentValueByName<T>(string? propertyName, T? value);

    /// <summary>
    /// Set the value as a specific type
    /// </summary>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    void SetCurrentValue<T>(T value);

    /// <summary>
    /// Create the current object if it is null
    /// </summary>
    void CreateCurrentIfNull();

    /// <summary>
    /// Clones the data context
    /// </summary>
    /// <returns>A new instance of the data context</returns>
    IDataContext Clone();
}