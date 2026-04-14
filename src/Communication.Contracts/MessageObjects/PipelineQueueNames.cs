namespace Meshmakers.Octo.Communication.Contracts.MessageObjects;

/// <summary>
/// Queue name constants for pipeline-related event hub commands
/// </summary>
public static class PipelineQueueNames
{
    /// <summary>
    /// Execute pipeline command
    /// </summary>
    public const string ExecutePipelineCommand = "octo::com-controller::execute-pipeline";
}
