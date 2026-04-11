namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Deployment state of a communication entity (adapter, pool, pipeline).
/// Values match the CK enum System.Communication/DeploymentState.
/// Note: This is distinct from <see cref="DeploymentState"/> which represents deployment operation results.
/// </summary>
public enum EntityDeploymentState
{
    /// <summary>
    /// Entity has not been deployed
    /// </summary>
    Undeployed = 0,

    /// <summary>
    /// Deployment is in progress
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Entity is deployed and active
    /// </summary>
    Deployed = 2,

    /// <summary>
    /// Deployment failed
    /// </summary>
    Error = 3
}
