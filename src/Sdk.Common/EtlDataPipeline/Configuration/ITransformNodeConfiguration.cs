namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;

/// <summary>
/// Base interface for transform node configuration
/// </summary>
public interface ITransformNodeConfiguration
{
    /// <summary>
    /// Gets or sets an optional description
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// The path to the source object as JSON path
    /// </summary>
    string? SourcePath { get; set; }
    
    /// <summary>
    /// Property name of the target object
    /// </summary>
    string? TargetPropertyName { get; set; }
}