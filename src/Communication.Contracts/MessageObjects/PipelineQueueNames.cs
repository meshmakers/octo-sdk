namespace Meshmakers.Octo.Communication.Contracts.MessageObjects;

/// <summary>
/// Queue name constants for pipeline-related event hub commands
/// </summary>
public static class PipelineQueueNames
{
    /// <summary>
    /// Execute pipeline command queue name
    /// </summary>
    public const string ExecutePipelineCommand = "octo::com-controller::execute-mesh-pipeline";
}
