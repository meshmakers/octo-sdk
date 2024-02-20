namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline;

/// <summary>
/// Pipeline node that processes data transformation
/// </summary>
public interface ITransformPipelineNode : IPipelineNode
{
    /// <summary>
    /// Processes an object.
    /// </summary>
    /// <param name="dataContext">Data context.</param>
    /// <returns></returns>
    Task ProcessObjectAsync(ITransformDataContext dataContext);
}