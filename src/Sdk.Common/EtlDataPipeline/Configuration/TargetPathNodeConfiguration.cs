namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;

/// <summary>
/// Pipeline node configuration including target path including write mode
/// </summary>
public record TargetPathNodeConfiguration : NodeConfiguration, ITargetPathNodeConfiguration
{
    /// <summary>
    /// Gets or sets the target path in json path format
    /// </summary>
    public string TargetPath { get; set; } = "$";
    
    /// <summary>
    /// Gets or sets the write mode (overwrite, append, prepend)
    /// </summary>
    public WriteMode TargetValueWriteMode { get; set; } = WriteMode.Overwrite;

    /// <summary>
    /// Gets or sets the value kind to write (simple value or array)
    /// </summary>
    public ValueKind TargetValueKind { get; set; } = ValueKind.Simple;
}