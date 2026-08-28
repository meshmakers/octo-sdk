namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Payload for the operator's <c>ScaleWorkloadAsync</c> callback (AB#4917, on-demand adapter
/// lifecycle AB#4914). The operator patches the replica count of the Kubernetes Deployments
/// belonging to the workload's Helm release — no <c>helm upgrade</c> involved, so scaling is
/// fast (~2 s) and does not touch the release history. Deployments are located via the
/// <c>app.kubernetes.io/instance={releaseName}</c> label; resource names are never derived
/// (Application charts may render <c>{release}-{chart}</c>).
/// </summary>
public record ScaleWorkloadDto
{
    /// <summary>
    /// Tenant the workload belongs to.
    /// </summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>
    /// Runtime entity id of the pool the workload is deployed under. Used for SignalR routing
    /// to the operator connection owning the pool (same contract as
    /// <see cref="WorkloadUndeployedDto.PoolRtId"/>).
    /// </summary>
    public string PoolRtId { get; init; } = string.Empty;

    /// <summary>
    /// Runtime entity id of the workload. The operator derives the Helm release name from it
    /// (same derivation as deploy/undeploy) to build the instance-label selector.
    /// </summary>
    public string WorkloadRtId { get; init; } = string.Empty;

    /// <summary>
    /// User-facing workload name. Display / event logging only.
    /// </summary>
    public string WorkloadName { get; init; } = string.Empty;

    /// <summary>
    /// Discriminator between <c>Adapter</c> and <c>Application</c>.
    /// </summary>
    public WorkloadTypeDto WorkloadType { get; init; }

    /// <summary>
    /// Desired replica count. The lifecycle feature only uses 0 (hibernate) and 1 (wake), but
    /// the wire contract is a plain count so it stays future-proof.
    /// </summary>
    public int Replicas { get; init; }
}
