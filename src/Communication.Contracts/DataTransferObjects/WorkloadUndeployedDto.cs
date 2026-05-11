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
    /// Pool that managed this workload.
    /// </summary>
    public string PoolName { get; init; } = string.Empty;

    /// <summary>
    /// Workload name (must match the one used at deploy time).
    /// </summary>
    public string WorkloadName { get; init; } = string.Empty;

    /// <summary>
    /// Discriminator between <c>Adapter</c> and <c>Application</c>.
    /// </summary>
    public WorkloadTypeDto WorkloadType { get; init; }
}
