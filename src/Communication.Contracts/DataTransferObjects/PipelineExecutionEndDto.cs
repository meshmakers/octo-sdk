namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// DTO for reporting pipeline execution end to the communication controller
/// </summary>
public record PipelineExecutionEndDto
{
    /// <summary>
    /// Unique identifier for this execution (GUID as string)
    /// </summary>
    public required string ExecutionId { get; init; }

    /// <summary>
    /// Final status of the execution
    /// </summary>
    public required PipelineExecutionStatus Status { get; init; }

    /// <summary>
    /// When the execution completed (UTC)
    /// </summary>
    public required DateTime CompletedAt { get; init; }

    /// <summary>
    /// Duration of the execution in milliseconds
    /// </summary>
    public required int DurationMs { get; init; }

    /// <summary>
    /// Error message if the execution failed
    /// </summary>
    public string? ErrorMessage { get; init; }
}
