namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Identifies a Cloud-environment pool that the central Communication Operator
/// must keep deployed. Used both as a callback payload (when a pool is
/// deployed/undeployed at runtime) and as the registration response (after
/// the operator (re)connects to the controller's <c>/operatorHub</c>).
/// </summary>
public record DeployedPoolDto
{
    /// <summary>
    /// Tenant the pool belongs to.
    /// </summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>
    /// Runtime entity id of the pool. The canonical pool identity on the
    /// wire and the source of truth for every derived Kubernetes
    /// identifier on the operator side (CommunicationPool CR
    /// <c>metadata.name</c>, broker secret name, identity labels).
    /// RtIds are 24-character lowercase hex strings — always RFC 1123
    /// valid without sanitisation. The human-readable pool display name
    /// lives on the controller's <c>RtPool.Name</c> attribute (visible
    /// in Studio) and is not sent over the wire.
    /// </summary>
    public string PoolRtId { get; init; } = string.Empty;
}
