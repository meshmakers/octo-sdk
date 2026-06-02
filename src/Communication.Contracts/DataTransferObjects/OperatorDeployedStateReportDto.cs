namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Self-healing reverse-sync payload sent by a freshly (re)connected Cloud
/// operator. Each entry lists a pool the operator currently has a healthy
/// helm release for, plus the workload runtime ids whose releases the
/// operator is actively managing inside that pool.
///
/// The controller restores <c>DeploymentState=Deployed</c> on every listed
/// pool / workload (when not already Deployed) and rebuilds its per-connection
/// tracking so undeploy fan-out works after the operator restart. Pools /
/// workloads omitted from the report are NOT touched — this is an additive
/// "what I currently own" handshake, not a state diff.
///
/// Edge operators must NOT call this contract; the controller's handler
/// rejects with a <c>HubException</c> when the operator's declared
/// <c>AutoManagePools</c> mode is anything other than <c>true</c>. The same
/// guard that gates pool-environment claims (<c>RtPool.Environment == Cloud</c>
/// + <c>OperatorMode == Cloud</c>) is enforced per pool inside the loop so a
/// rogue Cloud operator cannot revive Edge-pool state.
/// </summary>
public record OperatorDeployedPoolReportDto
{
    /// <summary>
    /// Tenant the pool belongs to.
    /// </summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>
    /// Runtime entity id of the pool the operator considers deployed. Same
    /// 24-char hex format as <see cref="DeployedPoolDto.PoolRtId"/>.
    /// </summary>
    public string PoolRtId { get; init; } = string.Empty;

    /// <summary>
    /// The pool's human-readable name as the operator knows it (mirrors the
    /// CR <c>metadata.labels.poolName</c>). Used for tracking-map keys so
    /// later undeploy fan-out can be wired without an additional repository
    /// lookup.
    /// </summary>
    public string PoolName { get; init; } = string.Empty;

    /// <summary>
    /// Runtime entity ids of every workload (Adapter / Application) the
    /// operator currently has an active helm release for inside this pool.
    /// Empty list is valid (pool deployed, no workloads currently running).
    /// </summary>
    public IReadOnlyList<string> WorkloadRtIds { get; init; } = Array.Empty<string>();
}
