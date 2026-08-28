namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Status report fired by the Communication Operator after a <c>ScaleWorkloadAsync</c> attempt
/// (AB#4917). The controller uses it to advance the workload's lifecycle state machine
/// (AB#4914): a successful scale-to-0 ack transitions <c>Draining → Hibernated</c>; a failed
/// scale-to-1 lets the wake gate fail fast instead of waiting for its full budget.
/// </summary>
public record WorkloadScaleStatusDto
{
    /// <summary>Tenant the workload belongs to.</summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>
    /// Runtime entity id of the workload (mirrors <see cref="ScaleWorkloadDto.WorkloadRtId"/>).
    /// </summary>
    public string WorkloadRtId { get; init; } = string.Empty;

    /// <summary>User-facing workload name. Display / event logging only.</summary>
    public string WorkloadName { get; init; } = string.Empty;

    /// <summary>The replica count that was requested.</summary>
    public int Replicas { get; init; }

    /// <summary>
    /// <c>true</c> when every Deployment of the release was patched to the requested replica
    /// count; <c>false</c> when the release had no Deployments or a patch failed.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Free-form human-readable message; on failure carries the Kubernetes API error.
    /// </summary>
    public string? StatusMessage { get; init; }
}
