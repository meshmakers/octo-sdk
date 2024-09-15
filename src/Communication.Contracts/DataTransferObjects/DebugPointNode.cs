namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Describes a node in the pipeline for debugging purposes
/// </summary>
public class DebugPointNode
{
    /// <summary>
    /// Gets or sets the sequence number of the node within a transformation list
    /// </summary>
    public uint SequenceNumber { get; set; } = default!;
    
    /// <summary>
    /// Full path of the node
    /// </summary>
    public NodePath FullPath { get; set; }

    /// <summary>
    /// Gets the node name
    /// </summary>
    public string Name { get; set; } = default!;
    
    /// <summary>
    /// Describes the node
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the children of the node
    /// </summary>
    public List<DebugPointNode>? Children { get; set; }
}