namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Debugger;

/// <summary>
/// The serialized debug information
/// </summary>
public class DebugInformationRoot
{
    /// <summary>
    /// Gets the debug messages
    /// </summary>
    public ICollection<DebugMessage> DebugMessages { get; set; } = null!;
    
    /// <summary>
    /// Gets the debug points
    /// </summary>
    public ICollection<DebugPoint> DebugPoints { get; set; }= null!;
}