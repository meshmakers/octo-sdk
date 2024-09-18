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
    /// <param name="nodePath">Node path</param>
    /// <param name="sequenceNumber">Sequence number of the node within a transformation list</param>
    [JsonConstructor]
    public DebugPointDto(NodePath nodePath, uint sequenceNumber)
    {
        NodePath = nodePath;
        SequenceNumber = sequenceNumber;
    }

    /// <summary>
    /// Gets the node path
    /// </summary>
    public NodePath NodePath { get; }

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