using System.Text.Json.Serialization;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Represents a debug point with the before and after data and the node configuration
/// </summary>
public record DebugPointDto
{
    /// <summary>
    /// Creates a new instance of <see cref="DebugPointDto"/>
    /// </summary>
    /// <param name="nodeId">Node id</param>
    /// <param name="nodePath">Node path</param>
    /// <param name="description">The description of the node</param>
    /// <param name="sequenceNumber">Sequence number of the node within a transformation list</param>
    [JsonConstructor]
    public DebugPointDto(string nodeId, NodePath nodePath, string? description, uint sequenceNumber)
    {
        NodeId = nodeId;
        NodePath = nodePath;
        Description = description;
        SequenceNumber = sequenceNumber;
    }

    /// <summary>
    /// Represents the id of debug point node
    /// </summary>
    public string NodeId { get; }

    /// <summary>
    /// Gets the node path
    /// </summary>
    public NodePath NodePath { get; }

    /// <summary>
    /// Gets or sets the description of the node
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the sequence number of the node within a transformation list
    /// </summary>
    public uint SequenceNumber { get; }

    /// <summary>
    /// Gets the input data
    /// </summary>
    public string? Input { get; set; }

    /// <summary>
    /// Gets or sets the output data
    /// </summary>
    public string? Output { get; set; }

    /// <summary>
    /// Gets or sets the debug messages
    /// </summary>
    public IEnumerable<DebugMessage>? Messages { get; set; }
}