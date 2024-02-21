namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline;

/// <summary>
/// Pipeline node that stores data in a target system.
/// </summary>
public interface ILoadPipelineNode : IPipelineNode
{
    /// <summary>
    /// Processes an object.
    /// </summary>
    /// <param name="dataContext">Data context.</param>
    /// <returns></returns>
    Task ProcessObjectAsync(ILoadDataContext dataContext);
}