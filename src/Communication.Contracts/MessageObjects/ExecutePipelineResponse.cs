namespace Meshmakers.Octo.Communication.Contracts.MessageObjects;

/// <summary>
/// Response after executing a mesh pipeline
/// </summary>
/// <param name="IsSuccessStartingExecution">Indicates if the execution started successfully</param>
/// <param name="ErrorMessage">An error message if the execution start failed</param>
/// <param name="PipelineExecutionId">The id of the pipeline execution</param>
/// <param name="ExecutionStartTime">Start time of the execution</param>
public record ExecutePipelineResponse(
    bool IsSuccessStartingExecution,
    string? ErrorMessage,
    Guid? PipelineExecutionId,
    DateTime? ExecutionStartTime);
