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

    /// <summary>
    /// Called when an Adapter or Application managed by a Cloud pool should
    /// be deployed (or re-deployed). The operator runs
    /// <c>helm upgrade --install</c> against the chart referenced by
    /// <see cref="WorkloadDeployedDto.RepositoryUrl"/> +
    /// <see cref="WorkloadDeployedDto.ChartName"/> +
    /// <see cref="WorkloadDeployedDto.ChartVersion"/>, using
    /// <see cref="WorkloadDeployedDto.ValuesYaml"/> as the base values and
    /// <see cref="WorkloadDeployedDto.Values"/> as structured overrides
    /// (deep-merged on top). Secret-flagged overrides arrive decrypted.
    /// </summary>
    Task WorkloadDeployedAsync(WorkloadDeployedDto workload);

    /// <summary>
    /// Called when an Adapter or Application should be undeployed. The
    /// operator runs <c>helm uninstall</c> for the matching release and
    /// removes the operator-owned secret if one was created at deploy time.
    /// </summary>
    Task WorkloadUndeployedAsync(WorkloadUndeployedDto workload);

    /// <summary>
    /// Fired by the controller before the tenant's CK model is reloaded /
    /// migrated. Mirrors the legacy <c>IPoolHubCallbacks.PreUpdateTenantAsync</c>
    /// signal; moved here so the operator only needs the single
    /// <c>/operatorHub</c> channel. Operators should let in-flight work
    /// settle and prepare to re-register their pools afterwards.
    /// </summary>
    Task PreUpdateTenantAsync(string tenantId);
}
