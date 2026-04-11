namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Summary of a pool's state for list display.
/// </summary>
public record PoolSummaryDto
{
    /// <summary>
    /// Runtime identifier of the pool
    /// </summary>
    public required string RtId { get; init; }

    /// <summary>
    /// Display name of the pool
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Optional description
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Whether the pool operator is connected to the communication service
    /// </summary>
    public required CommunicationState CommunicationState { get; init; }

    /// <summary>
    /// Whether the pool has been configured
    /// </summary>
    public required ConfigurationState ConfigurationState { get; init; }

    /// <summary>
    /// Whether the pool has been deployed
    /// </summary>
    public required EntityDeploymentState DeploymentState { get; init; }

    /// <summary>
    /// When the communication state last changed
    /// </summary>
    public DateTime? CommunicationStateTimestamp { get; init; }

    /// <summary>
    /// Status message from the pool
    /// </summary>
    public string? StatusMessage { get; init; }
}
