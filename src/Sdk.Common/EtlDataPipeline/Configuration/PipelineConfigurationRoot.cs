namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;

/// <summary>
/// The pipeline configuration root.
/// </summary>
public class PipelineConfigurationRoot
{
    /// <summary>
    /// The steps to extract data from the source 
    /// </summary>
    public ICollection<ExtractNodeConfiguration>? Extracts { get; set; }
    
    /// <summary>
    /// The steps to transform the data
    /// </summary>
    public ICollection<TransformNodeConfiguration>? Transformations { get; set; }
    
    /// <summary>
    /// The steps to load the data to the target 
    /// </summary>
    public ICollection<LoadNodeConfiguration>? Loads { get; set; }
}