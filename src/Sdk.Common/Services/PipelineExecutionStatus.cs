namespace Meshmakers.Octo.Sdk.Common.Services;

/// <summary>
/// Defines the pipeline execution service
/// </summary>
public enum PipelineExecutionStatus
{
    /// <summary>
    /// The pipeline execution is running
    /// </summary>
    Running,

    /// <summary>
    /// The pipeline execution has completed
    /// </summary>
    Completed,

    /// <summary>
    /// The pipeline execution has failed
    /// </summary>
    Failed
}