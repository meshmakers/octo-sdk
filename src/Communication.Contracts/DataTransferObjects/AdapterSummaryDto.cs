namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Summary of an adapter's state for list display.
/// </summary>
public record AdapterSummaryDto
{
    /// <summary>
    /// Runtime identifier of the adapter
    /// </summary>
    public required string RtId { get; init; }

    /// <summary>
    /// Display name of the adapter
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Optional description
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Whether the adapter is connected to the communication service
    /// </summary>
    public required CommunicationState CommunicationState { get; init; }

    /// <summary>
    /// Whether the adapter has been configured
    /// </summary>
    public required ConfigurationState ConfigurationState { get; init; }

    /// <summary>
    /// Whether the adapter has been deployed
    /// </summary>
    public required EntityDeploymentState DeploymentState { get; init; }

    /// <summary>
    /// When the communication state last changed
    /// </summary>
    public DateTime? CommunicationStateTimestamp { get; init; }

    /// <summary>
    /// Container image name (for managed adapters)
    /// </summary>
    public string? ImageName { get; init; }

    /// <summary>
    /// Container image version (for managed adapters)
    /// </summary>
    public string? ImageVersion { get; init; }

    /// <summary>
    /// Status message from the adapter
    /// </summary>
    public string? StatusMessage { get; init; }
}
