namespace Meshmakers.Octo.Sdk.Common.DataPipeline.Configuration;

/// <summary>
/// Transform configuration nodes
/// </summary>
public class TransformConfigurationNode : ConfigurationNode
{
    /// <summary>
    /// The path to the source object as JSON path
    /// </summary>
    public string SourcePath { get; set; } = "$";
    
    /// <summary>
    /// Property name of the target object
    /// </summary>
    public string? TargetPropertyName { get; set; }
}