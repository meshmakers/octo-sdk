using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

namespace Meshmakers.Octo.Communication.Contracts.Hubs;

/// <summary>
/// Callback interface for operator management connections.
/// The Communication Controller calls these methods on connected operators
/// to notify them of deployment site deploy / undeploy events for Cloud-environment deployment sites.
/// Edge-environment deployment sites are not pushed — those are installed and run by an
/// external operator outside the central cluster.
/// </summary>
public interface IOperatorHubCallbacks
{
    /// <summary>
    /// Called when a Cloud deployment site is deployed (or re-deployed). The operator
    /// should ensure the corresponding DeploymentSite CR and broker secret
    /// exist in its deployment site namespace.
    /// </summary>
    Task DeploymentSiteDeployedAsync(DeployedDeploymentSiteDto deploymentSite);

    /// <summary>
    /// Called when a Cloud deployment site is undeployed. The operator should remove
    /// the corresponding DeploymentSite CR and broker secret.
    /// <paramref name="deploymentSiteRtId"/> is the source of truth for locating the
    /// derived Kubernetes resources.
    /// </summary>
    Task DeploymentSiteUndeployedAsync(string tenantId, string deploymentSiteRtId);

    /// <summary>
    /// Called when an Adapter or Application managed by a Cloud deployment site should
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
    /// Called when an Adapter or Application should be scaled to a specific
    /// replica count without touching the Helm release (AB#4917, on-demand
    /// lifecycle AB#4914). The operator patches
    /// <c>{"spec":{"replicas":N}}</c> on every Deployment carrying the
    /// release's <c>app.kubernetes.io/instance</c> label and reports the
    /// outcome via <c>IOperatorHub.ReportWorkloadScaleStatusAsync</c>.
    /// Operators running an older build without this handler log an
    /// unbound-method warning and the controller degrades gracefully
    /// (once-only HubException pattern).
    /// </summary>
    Task ScaleWorkloadAsync(ScaleWorkloadDto workload);

    /// <summary>
    /// Fired by the controller before the tenant's CK model is reloaded /
    /// migrated. Mirrors the legacy <c>IPoolHubCallbacks.PreUpdateTenantAsync</c>
    /// signal; moved here so the operator only needs the single
    /// <c>/operatorHub</c> channel. Operators should let in-flight work
    /// settle and prepare to re-register their deployment sites afterwards.
    /// </summary>
    Task PreUpdateTenantAsync(string tenantId);
}
