namespace Meshmakers.Octo.Sdk.Common.DataPipeline;

/// <summary>
/// Data context for single values
/// </summary>
public interface ISignalDataContext : IDataContext
{
    /// <summary>
    /// The value to transform
    /// </summary>
    object? Value { get; }
    
    /// <summary>
    /// Get the value as a specific type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    T? GetValue<T>();
}