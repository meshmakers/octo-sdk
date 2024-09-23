namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;

/// <summary>
/// The pipeline configuration root.
/// </summary>
public class PipelineConfigurationRoot
{
    /// <summary>
    /// Extract nodes of the current pipeline
    /// </summary>
    public ICollection<ExtractNodeConfiguration>? Extracts { get; set; }
    
    /// <summary>
    /// Transformations of the current node
    /// </summary>
    public ICollection<NodeConfiguration>? Transformations { get; set; }
}
