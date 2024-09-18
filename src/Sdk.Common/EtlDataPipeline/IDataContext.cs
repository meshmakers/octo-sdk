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
    /// Returns the node context, that contains information about the current node
    /// </summary>
    INodeContext NodeContext { get; }

    /// <summary>
    /// Provider for services that are global for the whole application
    /// </summary>
    IServiceProvider GlobalServiceProvider { get; }

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
    /// Get the value as a specific type. The value is expected to be a simple value.
    /// </summary>
    /// <param name="path">Path to the value</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    T? GetSimpleValueByPath<T>(string? path);

    /// <summary>
    /// Deserialize the object of the current value
    /// </summary>
    /// <param name="path">JSON path of property to append to</param>
    /// <typeparam name="T">Type of the value</typeparam>
    /// <returns>The deserialized value</returns>
    T? GetComplexObjectByPath<T>(string? path);

    /// <summary>
    /// Deserialize the object of the current value
    /// </summary>
    /// <param name="path">JSON path of property to append to</param>
    /// <param name="jsonSerializer">JSON serializer to use</param>
    /// <typeparam name="T">Type of the value</typeparam>
    /// <returns>The deserialized value</returns>
    T? GetComplexObjectByPath<T>(string? path, JsonSerializer jsonSerializer);

    /// <summary>
    /// Get the value as a specific type. The value is expected to be an array.
    /// </summary>
    /// <param name="path">Property name</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    IEnumerable<T?>? GetSimpleArrayValueByPath<T>(string path);

    /// <summary>
    /// Set the value as a specific type
    /// </summary>
    /// <param name="path">Property name</param>
    /// <param name="valueKind">Defines if a value should be a simple value or array</param>
    /// <param name="writeMode">Defines if a value should be replaced or appended</param>
    /// <param name="value">Value to set</param>
    /// <typeparam name="T">Type of the value</typeparam>
    void SetValueByPath<T>(string? path, ValueKind valueKind, WriteMode writeMode, T? value);

    /// <summary>
    /// Set the value as a specific type
    /// </summary>
    /// <param name="path">Property name</param>
    /// <param name="value">Value to set</param>
    /// <param name="valueKind">Defines if a value should be a simple value or array</param>
    /// <param name="writeMode">Defines if a value should be replaced or appended</param>
    /// <param name="jsonSerializer">JSON serializer to use</param>
    /// <typeparam name="T">Type of the value</typeparam>
    void SetValueByPath<T>(string? path, T? value, ValueKind valueKind, WriteMode writeMode,
        JsonSerializer jsonSerializer);

    /// <summary>
    /// Register a node as a child of the current node
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="nodeQualifiedName"></param>
    /// <param name="sequenceNumber"></param>
    /// <param name="nodeConfiguration"></param>
    /// <returns></returns>
    INodeContext RegisterChildNode(INodeContext parent, string nodeQualifiedName, uint sequenceNumber, INodeConfiguration nodeConfiguration);

    /// <summary>
    /// Create the current object if it is null
    /// </summary>
    void CreateCurrentIfNull();

    /// <summary>
    /// Creates child data context of the current data context.
    /// </summary>
    /// <param name="input">The input value for the child context</param>
    /// <param name="parentNodeContext"></param>
    /// <param name="nodeQualifiedName"></param>
    /// <param name="sequenceNumber"></param>
    /// <param name="nodeConfiguration"></param>
    /// <returns></returns>
    (IDataContext, INodeContext) CreateSubContext(JToken? input, INodeContext parentNodeContext, string nodeQualifiedName, uint sequenceNumber,
        INodeConfiguration nodeConfiguration);
}