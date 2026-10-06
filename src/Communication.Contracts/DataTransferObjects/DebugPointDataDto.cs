using System.Text.Json;
using System.Text.Json.Serialization;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Represents a debug point with the before and after data and the node configuration
/// </summary>
/// <param name="nodeId">Node identifier</param>
/// <param name="nodePath">Node path</param>
/// <param name="description">Description of the node</param>
/// <param name="sequenceNumber">Sequence number of the node within a transformation list</param>
public class DebugPointDataDto(string nodeId, NodePath nodePath, string? description, uint sequenceNumber)
{
    /// <summary>
    /// Gets the node identifier
    /// </summary>
    public NodePath NodeId { get; } = nodeId;

    /// <summary>
    /// Gets the node path
    /// </summary>
    public NodePath NodePath { get; } = nodePath;

    /// <summary>
    /// Gets the description of the node
    /// </summary>
    public string? Description { get; } = description;

    /// <summary>
    /// Gets the sequence number of the node within a transformation list
    /// </summary>
    public uint SequenceNumber { get; } = sequenceNumber;

    /// <summary>
    /// Gets or sets the debug messages
    /// </summary>
    public IEnumerable<DebugMessage>? Messages { get; init; }

    /// <summary>
    /// Gets the input data
    /// </summary>
    public JsonElement? Input { get; init; }

    /// <summary>
    /// Gets the output data
    /// </summary>
    public JsonElement? Output { get; init; }

    /// <summary>
    /// Gets the JSONPaths (rooted at the snapshot object, e.g. <c>$.output.smtp.password</c>) whose value was
    /// redacted to <c>***</c> because it holds a secret (AB#5528). A value masked inside a longer string
    /// (e.g. <c>Bearer ***</c>) is listed with the path of the containing string. <c>null</c> when nothing
    /// was redacted or the producer does not report it; not written when <c>null</c>.
    /// </summary>
    [JsonPropertyName("redactedPaths")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Newtonsoft.Json.JsonProperty("redactedPaths", NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
    public IReadOnlyList<string>? RedactedPaths { get; init; }
}
