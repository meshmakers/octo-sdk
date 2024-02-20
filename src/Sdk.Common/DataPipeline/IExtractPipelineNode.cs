namespace Meshmakers.Octo.Sdk.Common.DataPipeline;

/// <summary>
/// Pipeline node that processes extract data.
/// </summary>
public interface IExtractPipelineNode : IPipelineNode
{
    /// <summary>
    /// Processes an object.
    /// </summary>
    /// <param name="dataContext">Data context.</param>
    /// <returns></returns>
    Task ProcessObjectAsync(IExtractDataContext dataContext);
}