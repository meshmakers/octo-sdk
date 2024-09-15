using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Debugger;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Newtonsoft.Json;
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
    /// Provider for logging
    /// </summary>
    IPipelineLogger Logger { get; }
    
    /// <summary>
    /// Gets the sequence number of the node within a transformation list.
    /// </summary>
    public uint SequenceNumber { get; }

    /// <summary>
    /// Get configuration for the current node
    /// </summary>
    /// <typeparam name="T">Generic type of configuration</typeparam>
    /// <returns></returns>
    T GetNodeConfiguration<T>() where T : INodeConfiguration;

    /// <summary>
    /// Gets the pipeline debugger if configured
    /// </summary>
    /// <returns></returns>
    IPipelineDebugger? Debugger { get; }
    
    /// <summary>
    /// The current pipeline object. This is the object that is being processed by the pipeline in the transform stage.
    /// </summary>
    public JToken? Current { get; set; }

    /// <summary>
    /// Returns the path queue
    /// </summary>
    public Stack<NodePath> NodeStack { get; }
    
    /// <summary>
    /// Get the value as a specific type
    /// </summary>
    /// <param name="path">Path to the value</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    T? GetCurrentValueByPath<T>(string? path);
    
    /// <summary>
    /// Get the value as a specific type
    /// </summary>
    /// <param name="path">Property name</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    IEnumerable<T?>? GetCurrentValuesByPath<T>(string path);

    /// <summary>
    /// Set the value as a specific type
    /// </summary>
    /// <param name="path">Property name</param>
    /// <param name="value">Value to set</param>
    /// <typeparam name="T">Type of the value</typeparam>
    void SetCurrentValueByPath<T>(string? path, T? value);

    /// <summary>
    /// Set the value as a specific type
    /// </summary>
    /// <param name="path">Property name</param>
    /// <param name="value">Value to set</param>
    /// <param name="jsonSerializer">JSON serializer to use</param>
    /// <typeparam name="T">Type of the value</typeparam>
    void SetCurrentValueByPath<T>(string? path, T? value, JsonSerializer jsonSerializer);
    
    /// <summary>
    /// Set the value as a specific type
    /// </summary>
    /// <param name="value">Value to set</param>
    /// <typeparam name="T">Type of the value</typeparam>
    void SetCurrentValue<T>(T value);

    /// <summary>
    /// Set the value as a specific type
    /// </summary>
    /// <param name="value">Value to set</param>
    /// <param name="jsonSerializer">JSON serializer to use</param>
    /// <typeparam name="T">Type of the value</typeparam>
    void SetCurrentValue<T>(T value, JsonSerializer jsonSerializer);
    
    /// <summary>
    /// Append the value to an array
    /// </summary>
    /// <param name="path">JSON path of property to append to</param>
    /// <param name="value">Value to append</param>
    /// <typeparam name="T">Type of the value</typeparam>
    void AppendToCurrentValue<T>(string path, T value);

    /// <summary>
    /// Deserialize the object of the current value
    /// </summary>
    /// <param name="path">JSON path of property to append to</param>
    /// <typeparam name="T">Type of the value</typeparam>
    /// <returns>The deserialized value</returns>
    T? DeserializeCurrentValue<T>(string? path);
    
    /// <summary>
    /// Deserialize the object of the current value
    /// </summary>
    /// <param name="path">JSON path of property to append to</param>
    /// <param name="jsonSerializer">JSON serializer to use</param>
    /// <typeparam name="T">Type of the value</typeparam>
    /// <returns>The deserialized value</returns>
    T? DeserializeCurrentValue<T>(string? path, JsonSerializer jsonSerializer);
    
    /// <summary>
    /// Create the current object if it is null
    /// </summary>
    void CreateCurrentIfNull();
}