namespace Meshmakers.Octo.Sdk.Common.DataPipeline;

/// <summary>
/// Pipeline node that processes objects.
/// </summary>
public interface IObjectPipelineNode :IPipelineNode
{
    /// <summary>
    /// Processes an object.
    /// </summary>
    /// <param name="dataContext">Data context.</param>
    /// <returns></returns>
    Task<object?> ProcessObjectAsync(IObjectDataContext dataContext);
}