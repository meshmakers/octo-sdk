namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Data transfer object describing a pipeline node type with its configuration schema.
/// Sent from adapters to the communication controller during registration.
/// </summary>
/// <param name="NodeName">The name of the node (e.g. "Select")</param>
/// <param name="Version">The version of the node</param>
/// <param name="Category">The category (e.g. "Trigger", "Transform", "Control")</param>
/// <param name="IsTrigger">Whether this node is a trigger node</param>
/// <param name="SupportsChildren">Whether this node supports child transformations</param>
/// <param name="ConfigurationSchemaJson">JSON Schema string describing the configuration</param>
/// <param name="IsDeprecated">Whether this node is deprecated</param>
/// <param name="DeprecationMessage">Optional reason or migration hint when the node is deprecated</param>
public record NodeDescriptorDto(
    string NodeName,
    int Version,
    string Category,
    bool IsTrigger,
    bool SupportsChildren,
    string ConfigurationSchemaJson,
    bool IsDeprecated = false,
    string? DeprecationMessage = null);
