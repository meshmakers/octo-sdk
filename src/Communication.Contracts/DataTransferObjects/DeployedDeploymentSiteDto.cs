namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Identifies a Cloud-environment deployment site that the central Communication Operator
/// must keep deployed. Used both as a callback payload (when a deployment site is
/// deployed/undeployed at runtime) and as the registration response (after
/// the operator (re)connects to the controller's <c>/operatorHub</c>).
/// </summary>
public record DeployedDeploymentSiteDto
{
    /// <summary>
    /// Tenant the deployment site belongs to.
    /// </summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>
    /// Runtime entity id of the deployment site. The canonical deployment site identity on the
    /// wire and the source of truth for every derived Kubernetes
    /// identifier on the operator side (DeploymentSite CR
    /// <c>metadata.name</c>, broker secret name, identity labels).
    /// RtIds are 24-character lowercase hex strings — always RFC 1123
    /// valid without sanitisation. The human-readable deployment site display name
    /// lives on the controller's <c>RtDeploymentSite.Name</c> attribute (visible
    /// in Studio) and is not sent over the wire.
    /// </summary>
    public string DeploymentSiteRtId { get; init; } = string.Empty;
}
