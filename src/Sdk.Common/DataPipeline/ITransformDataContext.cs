using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.DataPipeline;

/// <summary>
/// Interface for data context of transform stage
/// </summary>
public interface ITransformDataContext : IDataContext
{
    /// <summary>
    /// The source of the data from the extraction process. Can be used to pass data between nodes.
    /// </summary>
    public JToken? Source { get; }
    
    /// <summary>
    /// The current pipeline object. This is the object that is being processed by the pipeline in the transform stage.
    /// </summary>
    public JToken Target { get; }
    
    /// <summary>
    /// Get the value as a specific type
    /// </summary>
    /// <param name="path">Path to the value</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    T? GetSourceValueByPath<T>(string path);

    /// <summary>
    /// Set the value as a specific type
    /// </summary>
    /// <param name="propertyName">Path to the value</param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    void SetTargetValueByName<T>(string? propertyName, T value);

    /// <summary>
    /// Set the value as a specific type
    /// </summary>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    void SetTargetValue<T>(T value);
}