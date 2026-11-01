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

    /// <summary>
    /// What a failed execution says about its writes (AB#8037): <see cref="PipelineExecutionFailureKind.OutcomeUnknown" />
    /// when a write was not acknowledged and may have taken effect, <see cref="PipelineExecutionFailureKind.Failed" />
    /// for every other failure. Null when the status is not <see cref="PipelineExecutionStatus.Failed" /> and on an
    /// adapter built before this field existed; the controller treats null like
    /// <see cref="PipelineExecutionFailureKind.Failed" />.
    /// </summary>
    public PipelineExecutionFailureKind? FailureKind { get; init; }

    /// <summary>
    /// Optional output data (JSON string) from pipeline execution result
    /// </summary>
    public string? OutputData { get; init; }
}
