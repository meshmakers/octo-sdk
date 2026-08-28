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

    /// <summary>
    /// Durable controller-owned queue that receives <see cref="LifecycleWakeMessage"/>s from
    /// the cron co-wake companion schedules (AB#4914/AB#4918). Unlike the execute-pipeline
    /// queue this endpoint must be durable — a wake tick fired while the controller is
    /// restarting must survive and be consumed afterwards.
    /// </summary>
    public const string LifecycleWakeQueue = "octo::com-controller::lifecycle-wake";
}
