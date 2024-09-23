using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline;

/// <summary>
/// Interface for extract node context
/// </summary>
public interface IExtractNodeContext
{
    /// <summary>
    /// Returns the node context, that contains information about the current node
    /// </summary>
    INodeContext NodeContext { get; }
    
    /// <summary>
    /// Triggers the execution of the recent transformation pipeline
    /// </summary>
    /// <returns></returns>
    Task ExecuteAsync(object? input = null);
}