namespace Meshmakers.Octo.Communication.Contracts.MessageObjects;

/// <summary>
///     Cron tick for a pipeline whose adapter is <c>Leased</c> (AB#5278, Epic AB#4914).
///     A leased adapter has no process of its own, so the per-pipeline trigger queue a dedicated
///     adapter consumes through <c>FromPipelineTriggerEvent@1</c> has no consumer — and a pool
///     member that happens to hold a lease must not consume it either, because a run that starts
///     that way bypasses the lease bookkeeping (no queued execution, no queue position, no lease
///     span). For such a pipeline the controller therefore registers the recurring send with THIS
///     message to its own durable queue (<see cref="PipelineQueueNames.LeaseTriggerQueue"/>)
///     instead of the adapter's queue, and turns every tick into a queued work item exactly as an
///     explicit ExecutePipeline would.
/// </summary>
/// <param name="TenantId">Tenant the pipeline belongs to (the borrower).</param>
/// <param name="PipelineRtId">Runtime entity id of the pipeline the cron fires.</param>
/// <param name="TriggerRtId">Runtime entity id of the <c>PipelineTrigger</c> that owns the cron, for diagnostics.</param>
public record LeaseTriggerMessage(string TenantId, string PipelineRtId, string TriggerRtId);
