namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline;

/// <summary>
/// Base interface for all pipeline nodes.
/// </summary>
public interface IPipelineNode
{
    /// <summary>
    /// Processes an object.
    /// </summary>
    /// <param name="dataContext">Data context.</param>
    /// <returns></returns>
    Task ProcessObjectAsync(IDataContext dataContext);
}