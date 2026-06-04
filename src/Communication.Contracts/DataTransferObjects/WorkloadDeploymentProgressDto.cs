namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Live-progress signal fired by the Communication Operator while a
/// workload <c>helm upgrade --install</c> is still in flight. Unlike
/// <see cref="WorkloadDeploymentStatusDto"/> — which carries the
/// terminal outcome — this DTO only refreshes
/// <c>StatusMessage</c> on the workload entity and leaves
/// <c>DeploymentState</c> at <c>Pending</c>.
///
/// Purpose: surface root causes (ImagePullBackOff, FailedScheduling,
/// CrashLoopBackOff, …) in the UI within seconds, instead of waiting
/// for the helm <c>--atomic</c> timeout (default 5 min) to elapse
/// before the operator reports a terminal failure.
/// </summary>
public record WorkloadDeploymentProgressDto
{
    /// <summary>Tenant the workload belongs to.</summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>Workload (CK <c>Name</c>) the progress refers to.</summary>
    public string WorkloadName { get; init; } = string.Empty;

    /// <summary>
    /// Runtime entity id of the workload (mirrors the
    /// <see cref="WorkloadDeployedDto.WorkloadRtId"/> the controller
    /// sent in the original deploy event).
    /// </summary>
    public string WorkloadRtId { get; init; } = string.Empty;

    /// <summary>
    /// Diagnostic snapshot collected from the cluster while the install
    /// is running. Same shape as
    /// <see cref="WorkloadDeploymentStatusDto.StatusMessage"/> on
    /// failure — typically one or more lines of
    /// <c>Pod X container 'Y' waiting: ImagePullBackOff — …</c> plus
    /// matching warning events.
    /// </summary>
    public string Message { get; init; } = string.Empty;
}
