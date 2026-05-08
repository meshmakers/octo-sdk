using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

namespace Meshmakers.Octo.Communication.Contracts.Hubs;

/// <summary>
/// Callback interface for operator management connections.
/// The Communication Controller calls these methods on connected operators
/// to notify them of pool deploy / undeploy events for Cloud-environment pools.
/// Edge-environment pools are not pushed — those are installed and run by an
/// external operator outside the central cluster.
/// </summary>
public interface IOperatorHubCallbacks
{
    /// <summary>
    /// Called when a Cloud pool is deployed (or re-deployed). The operator
    /// should ensure the corresponding CommunicationPool CR and broker secret
    /// exist in its pool namespace.
    /// </summary>
    Task PoolDeployedAsync(DeployedPoolDto pool);

    /// <summary>
    /// Called when a Cloud pool is undeployed. The operator should remove the
    /// corresponding CommunicationPool CR and broker secret.
    /// </summary>
    Task PoolUndeployedAsync(string tenantId, string poolName);
}
