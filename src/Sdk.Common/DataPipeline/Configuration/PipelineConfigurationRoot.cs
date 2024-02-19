namespace Meshmakers.Octo.Sdk.Common.DataPipeline.Configuration;

/// <summary>
/// The pipeline configuration root.
/// </summary>
public class PipelineConfigurationRoot
{
    /// <summary>
    /// The steps of the pipeline.
    /// </summary>
    public ICollection<ConfigurationNode>? TransformList { get; set; }
}