namespace Meshmakers.Octo.Sdk.Common.DataPipeline.Configuration;

/// <summary>
/// The pipeline configuration root.
/// </summary>
public class PipelineConfigurationRoot
{
    /// <summary>
    /// The steps to extract data from the source 
    /// </summary>
    public ICollection<ExtractConfigurationNode>? Extracts { get; set; }
    
    /// <summary>
    /// The steps to transform the data
    /// </summary>
    public ICollection<TransformConfigurationNode>? Transforms { get; set; }
    
    /// <summary>
    /// The steps to load the data to the target 
    /// </summary>
    public ICollection<LoadConfigurationNode>? Loads { get; set; }
}