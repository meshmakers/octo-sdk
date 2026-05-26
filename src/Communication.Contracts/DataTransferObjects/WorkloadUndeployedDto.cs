namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Payload for the operator's <c>WorkloadUndeployedAsync</c> callback. The
/// operator runs <c>helm uninstall</c> for the matching release and removes
/// any operator-owned Kubernetes <c>Secret</c> it created at deploy time.
/// </summary>
public record WorkloadUndeployedDto
{
    /// <summary>
    /// Tenant the workload belongs to.
    /// </summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>
    /// Runtime entity id of the pool the workload was deployed under. The
    /// canonical pool identity on the wire; the operator uses it to scope
    /// SignalR routing and to look up tracked pool resources.
    /// </summary>
    public string PoolRtId { get; init; } = string.Empty;

    /// <summary>
    /// Runtime entity id of the workload. Must match the value supplied
    /// at deploy time — the operator derives the Helm release name from
    /// it to locate the release to uninstall.
    /// </summary>
    public string WorkloadRtId { get; init; } = string.Empty;

    /// <summary>
    /// User-facing workload name. Preserved for display / event logging;
    /// not used to derive any Kubernetes identifier.
    /// </summary>
    public string WorkloadName { get; init; } = string.Empty;

    /// <summary>
    /// Discriminator between <c>Adapter</c> and <c>Application</c>.
    /// </summary>
    public WorkloadTypeDto WorkloadType { get; init; }
}
