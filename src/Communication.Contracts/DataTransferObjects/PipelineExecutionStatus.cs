namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Status of a pipeline execution.
/// Values must match the CK model enum PipelineExecutionStatus.
/// </summary>
public enum PipelineExecutionStatus
{
    /// <summary>
    /// Pipeline is currently running
    /// </summary>
    Running = 0,

    /// <summary>
    /// Pipeline completed successfully
    /// </summary>
    Completed = 1,

    /// <summary>
    /// Pipeline execution failed
    /// </summary>
    Failed = 2,

    /// <summary>
    /// Pipeline execution was interrupted (e.g., adapter disconnected)
    /// </summary>
    Interrupted = 3,

    /// <summary>
    /// Pipeline execution was cancelled
    /// </summary>
    Cancelled = 4,
}
