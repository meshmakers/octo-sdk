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
    /// Runtime entity id of the pool. Drives every derived Kubernetes
    /// identifier on the operator side (CommunicationPool CR
    /// <c>metadata.name</c>, the per-pool broker secret name, identity
    /// labels). RtIds are 24-character lowercase hex strings, so they are
    /// always RFC 1123 valid without sanitisation — they replaced the
    /// user-facing pool name there because the latter can contain
    /// whitespace, uppercase letters, or other characters the apiserver
    /// rejects with a 422.
    /// </summary>
    public string PoolRtId { get; init; } = string.Empty;

    /// <summary>
    /// User-facing pool name from the CK entity. Preserved verbatim for
    /// display in the Studio UI and for SignalR lookup keys on the
    /// controller side; not used to derive any Kubernetes identifier on
    /// the operator side. May contain whitespace and mixed case.
    /// </summary>
    public string PoolName { get; init; } = string.Empty;
}
