using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline;

/// <summary>
/// Base interface for data context
/// </summary>
public interface IDataContext
{
    /// <summary>
    /// Reference to the parent data context. If it is null, then it is the root data context.
    /// </summary>
    IDataContext? Parent { get; }

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
    /// Return true if the path is a simple array value or can be converted to a simple array value
    /// </summary>
    /// <param name="path">JSON path of property</param>
    /// <returns></returns>
    bool IsPathSimpleArrayValue(string? path);

    /// <summary>
    /// Get the value as a specific type. The value is expected to be an array.
    /// </summary>
    /// <param name="path">JSON path of property</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    IEnumerable<T?>? GetSimpleArrayValueByPath<T>(string path);
    
    /// <summary>
    /// Get the value as a specific type. The value is expected to be an array.
    /// </summary>
    /// <param name="path">JSON path of property</param>
    /// <returns></returns>
    IEnumerable<JToken> SelectByPath(string path);

    /// <summary>
    /// Set the value as a specific type
    /// </summary>
    /// <param name="path">JSON path of property</param>
    /// <param name="documentModes">Defines if the target document is extended or replaced</param>
    /// <param name="valueKinds">Defines if a value should be a simple value or array</param>
    /// <param name="targetValueWriteModes">Defines if a value should be replaced or appended</param>
    /// <param name="value">Value to set</param>
    /// <typeparam name="T">Type of the value</typeparam>
    void SetValueByPath<T>(string? path, DocumentModes documentModes, ValueKinds valueKinds, TargetValueWriteModes targetValueWriteModes, T? value);

    /// <summary>
    /// Set the value as a specific type
    /// </summary>
    /// <param name="path">JSON path of property</param>
    /// <param name="value">Value to set</param>
    /// <param name="documentModes">Defines if the target document is extended or replaced</param>
    /// <param name="valueKinds">Defines if a value should be a simple value or array</param>
    /// <param name="targetValueWriteModes">Defines if a value should be replaced or appended</param>
    /// <param name="jsonSerializer">JSON serializer to use</param>
    /// <typeparam name="T">Type of the value</typeparam>
    void SetValueByPath<T>(string? path, T? value, DocumentModes documentModes, ValueKinds valueKinds, TargetValueWriteModes targetValueWriteModes,
        JsonSerializer jsonSerializer);

    /// <summary>
    /// Create the current object if it is null
    /// </summary>
    void CreateCurrentIfNull();

    /// <summary>
    /// Create a child data context with the specified input
    /// </summary>
    /// <param name="input">JSON input for the child context</param>
    /// <returns>New data context</returns>
    IDataContext CreateChildDataContext(JToken input);
}