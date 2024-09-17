namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;

/// <summary>
/// Interface for target path node configuration
/// </summary>
public interface ITargetPathNodeConfiguration
{
    /// <summary>
    /// Gets or sets the target path in json path format
    /// </summary>
    string TargetPath { get; set; } 
    
    /// <summary>
    /// Gets or sets the write mode (overwrite, append, prepend)
    /// </summary>
    WriteMode TargetValueWriteMode { get; set; }

    /// <summary>
    /// Gets or sets the value kind to write (simple value or array)
    /// </summary>
    ValueKind TargetValueKind { get; set; } 
}