namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Status report fired by the Communication Operator after a workload
/// <c>helm upgrade --install</c> attempt. The controller writes
/// <see cref="Success"/> + <see cref="StatusMessage"/> back onto the
/// workload's runtime entity so users can see in the UI whether the
/// last deploy actually landed.
///
/// The operator-side helm error is otherwise only visible in operator
/// logs — without this round-trip the workload's <c>DeploymentState</c>
/// stays on whatever the controller set when it kicked off the deploy.
/// </summary>
public record WorkloadDeploymentStatusDto
{
    /// <summary>Tenant the workload belongs to.</summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>Pool that manages the workload.</summary>
    public string PoolName { get; init; } = string.Empty;

    /// <summary>Workload (CK <c>Name</c>) the report refers to.</summary>
    public string WorkloadName { get; init; } = string.Empty;

    /// <summary>
    /// Runtime entity id of the workload (mirrors the
    /// <see cref="WorkloadDeployedDto.WorkloadRtId"/> the controller
    /// sent in the original deploy event). Used by the controller to
    /// resolve the right MongoDB document without ambiguous name
    /// lookups.
    /// </summary>
    public string WorkloadRtId { get; init; } = string.Empty;

    /// <summary>
    /// <c>true</c> when <c>helm upgrade --install</c> exited cleanly
    /// (mapped to <c>RtDeploymentStateEnum.Deployed</c>); <c>false</c>
    /// when it threw (mapped to <c>RtDeploymentStateEnum.Error</c>).
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Free-form human-readable message. On failure this carries the
    /// helm stderr (or exception message) so the UI can show it; on
    /// success it's typically null or a short "deployed" string.
    /// </summary>
    public string? StatusMessage { get; init; }
}
