namespace Meshmakers.Octo.Sdk.Common.DataPipeline;

/// <summary>
/// Interface for data context of extract stage
/// </summary>
public interface IExtractDataContext : IDataContext
{
    /// <summary>
    /// The source of the data from the extraction process. Can be used to pass data between nodes.
    /// </summary>
    public object? Source { get; set; }
}