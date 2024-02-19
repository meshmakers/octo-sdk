namespace Meshmakers.Octo.Sdk.Common.DataPipeline;

/// <summary>
/// Interface for data context of an object
/// </summary>
public interface IObjectDataContext : IDataContext
{
    /// <summary>
    /// The source object
    /// </summary>
    object Source { get; }
}