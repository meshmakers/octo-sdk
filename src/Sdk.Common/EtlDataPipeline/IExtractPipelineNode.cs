namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline;

/// <summary>
/// Interface for extract pipeline nodes
/// </summary>
public interface IExtractPipelineNode
{
    /// <summary>
    /// Registers the data context.
    /// </summary>
    /// <param name="extractNodeContext"></param>
    /// <returns></returns>
    Task RegisterAsync(IExtractNodeContext extractNodeContext);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="extractNodeContext"></param>
    /// <returns></returns>
    Task UnregisterAsync(IExtractNodeContext extractNodeContext);
}