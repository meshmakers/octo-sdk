using Meshmakers.Octo.ConstructionKit.Contracts;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// DTO for reporting pipeline execution start to the communication controller
/// </summary>
public record PipelineExecutionStartDto
{
    /// <summary>
    /// Unique identifier for this execution (GUID as string)
    /// </summary>
    public required string ExecutionId { get; init; }

    /// <summary>
    /// Pipeline being executed
    /// </summary>
    public required RtEntityId PipelineRtEntityId { get; init; }

    /// <summary>
    /// Trigger type for this execution
    /// </summary>
    public required PipelineTriggerType TriggerType { get; init; }

    /// <summary>
    /// When the execution started (UTC)
    /// </summary>
    public required DateTime StartedAt { get; init; }

    /// <summary>
    /// Optional input data for debugging (JSON string, may be truncated)
    /// </summary>
    public string? InputData { get; init; }
}
