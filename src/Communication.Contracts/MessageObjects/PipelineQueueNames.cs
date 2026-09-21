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

    /// <summary>
    /// Durable controller-owned queue that receives <see cref="LeaseTriggerMessage"/>s from the
    /// cron schedules of pipelines on <c>Leased</c> adapters (AB#5278). Durable for the same
    /// reason as <see cref="LifecycleWakeQueue"/>: a tick fired while the controller restarts is
    /// a work item the borrower expects to see queued, not a message to lose.
    /// </summary>
    public const string LeaseTriggerQueue = "octo::com-controller::lease-trigger";
}
