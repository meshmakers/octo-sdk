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
    /// Gets or sets the writing mode (overwrite, append, prepend)
    /// </summary>
    public TargetValueWriteModes TargetValueWriteMode { get; set; } = TargetValueWriteModes.Overwrite;

    /// <summary>
    /// Gets or sets the value kind to write (simple value or array)
    /// </summary>
    public ValueKinds TargetValueKind { get; set; } = ValueKinds.Simple;

    /// <summary>
    /// Gets or sets the document mode (extend, replace)
    /// </summary>
    public DocumentModes DocumentMode { get; set; } = DocumentModes.Extend;
}