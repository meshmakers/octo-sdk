namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Describes a node in the pipeline for debugging purposes
/// </summary>
public class DebugPointNode
{
    /// <summary>
    /// Represents the id of debug point node
    /// </summary>
    public required string NodeId { get; set; }

    /// <summary>
    /// Gets or sets the sequence number of the node within a transformation list
    /// </summary>
    public required uint SequenceNumber { get; set; }
    
    /// <summary>
    /// Full path of the node
    /// </summary>
    public NodePath FullPath { get; set; }

    /// <summary>
    /// Gets the node name
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// Describes the node
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the children of the node
    /// </summary>
    public List<DebugPointNode>? Children { get; set; }
}