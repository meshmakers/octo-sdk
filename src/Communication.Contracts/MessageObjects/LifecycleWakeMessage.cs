namespace Meshmakers.Octo.Communication.Contracts.MessageObjects;

/// <summary>
///     Cron co-wake message for the on-demand adapter lifecycle (AB#4914/AB#4918).
///     For every cron <c>PipelineTrigger</c> whose pipeline runs on an <c>OnDemand</c> workload,
///     the controller registers a companion recurring send (same cron expression) carrying this
///     message to the controller-owned durable wake queue
///     (<see cref="PipelineQueueNames.LifecycleWakeQueue"/>). The controller consumes it and
///     wakes the workload, while the pipeline's own trigger message buffers durably on its
///     per-pipeline trigger queue and is consumed once the adapter is up.
/// </summary>
/// <param name="TenantId">Tenant the workload belongs to.</param>
/// <param name="WorkloadRtId">Runtime entity id of the workload (adapter) to wake.</param>
public record LifecycleWakeMessage(string TenantId, string WorkloadRtId);
