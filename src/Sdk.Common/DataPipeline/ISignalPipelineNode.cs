namespace Meshmakers.Octo.Sdk.Common.DataPipeline;

/// <summary>
/// Base interface for all pipeline nodes that process signals.
/// </summary>
public interface ISignalPipelineNode : IPipelineNode
{
    /// <summary>
    /// Processes the signal.
    /// </summary>
    /// <param name="dataContext">The data context.</param>
    /// <returns></returns>
    Task<object?> ProcessSignalAsync(ISignalDataContext dataContext);
}