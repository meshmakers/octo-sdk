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
    /// Pool name. Becomes part of the CommunicationPool CR name and the
    /// per-pool broker secret name in the operator's pool namespace.
    /// </summary>
    public string PoolName { get; init; } = string.Empty;
}
