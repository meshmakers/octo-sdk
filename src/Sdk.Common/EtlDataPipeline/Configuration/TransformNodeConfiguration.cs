namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;

/// <summary>
/// Transform configuration nodes
/// </summary>
public abstract class TransformNodeConfiguration : NodeConfiguration, ITransformNodeConfiguration
{
    /// <summary>
    /// The path to the source object as JSON path
    /// </summary>
    public string? SourcePath { get; set; }
    
    /// <summary>
    /// Property name of the target object
    /// </summary>
    public string? TargetPropertyName { get; set; }
}