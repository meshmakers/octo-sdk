using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

namespace Meshmakers.Octo.Communication.Contracts.Hubs;

/// <summary>
/// Server-side hub interface for operator management connections.
/// Used by the central Communication Operator to register for Cloud deployment site
/// deploy / undeploy notifications.
/// </summary>
public interface IOperatorHub
{
    /// <summary>
    /// Registers the operator for receiving Cloud deployment site deploy / undeploy
    /// events.
    /// </summary>
    /// <param name="autoManageDeploymentSites">
    /// The calling operator's <c>AutoManageDeploymentSites</c> setting:
    /// <c>true</c> = central operator (creates / deletes CRs in response to
    /// controller broadcasts), <c>false</c> = edge operator (CRs are managed
    /// out-of-band on the edge cluster). The controller stores this per
    /// connection and validates it against <c>RtDeploymentSite.Environment</c> on every
    /// <see cref="RegisterDeploymentSiteAsync"/> call so a Cloud deployment site cannot be claimed
    /// by an edge operator (and vice versa). <c>null</c> means the operator
    /// did not declare a mode (legacy build pre-dating this parameter); the
    /// controller logs and audit-records the registration but does not
    /// enforce mode/environment matching, so a rolling upgrade does not break
    /// existing connections.
    /// </param>
    /// <returns>
    /// All currently-deployed Cloud deployment sites across every tenant, so a freshly
    /// (re)connected operator can synchronize its desired state without
    /// missing deployment sites that were deployed while it was offline.
    /// </returns>
    Task<IEnumerable<DeployedDeploymentSiteDto>> RegisterOperatorAsync(bool? autoManageDeploymentSites = null);

    /// <summary>
    /// Unregisters the operator from receiving deployment site deploy / undeploy events.
    /// </summary>
    Task UnregisterOperatorAsync();

    /// <summary>
    /// Self-healing reverse-sync called by a Cloud operator after
    /// <see cref="RegisterOperatorAsync"/> on a fresh connection. The
    /// operator reports every deployment site / workload it currently has a healthy
    /// helm release for, and the controller restores
    /// <c>DeploymentState=Deployed</c> on any entity that is not already
    /// Deployed — closing the "operator restarts → tracking lost,
    /// CommunicationState ≠ DeploymentState" gap without requiring a
    /// human to re-click Deploy.
    ///
    /// Edge operators (<c>AutoManageDeploymentSites=false</c>) and operators that
    /// did not declare a mode are rejected with a <c>HubException</c>;
    /// the Cloud-only restriction matches the existing deployment site-environment
    /// enforcement on <see cref="RegisterDeploymentSiteAsync"/>. Deployment sites whose
    /// <c>Environment</c> is not Cloud are silently skipped inside the
    /// handler.
    /// </summary>
    Task ReportDeployedStateAsync(IReadOnlyList<OperatorDeployedDeploymentSiteReportDto> deployedDeploymentSites);

    /// <summary>
    /// Reports the outcome of a per-workload <c>helm upgrade --install</c>
    /// back to the controller. The controller writes the result onto the
    /// runtime entity's <c>DeploymentState</c> / <c>StatusMessage</c>
    /// attributes so the UI reflects what actually happened in the
    /// cluster — without this call, a failed helm run would only be
    /// visible in operator logs.
    /// </summary>
    Task ReportWorkloadDeploymentStatusAsync(WorkloadDeploymentStatusDto status);

    /// <summary>
    /// Live progress report fired while a <c>helm upgrade --install</c> is
    /// still in flight. The operator polls the cluster for failure-relevant
    /// pod / event signals (ImagePullBackOff, FailedScheduling,
    /// CrashLoopBackOff, …) and pushes them through this channel so the UI
    /// reflects the root cause within seconds, rather than waiting for the
    /// terminal <see cref="ReportWorkloadDeploymentStatusAsync"/> at the
    /// end of helm's atomic timeout.
    ///
    /// Controller writes <c>StatusMessage</c> only and leaves
    /// <c>DeploymentState</c> at <c>Pending</c> — helm may still recover
    /// (e.g. transient registry outage), so the terminal state machine
    /// stays owned by <see cref="ReportWorkloadDeploymentStatusAsync"/>.
    /// </summary>
    Task ReportWorkloadDeploymentProgressAsync(WorkloadDeploymentProgressDto progress);

    /// <summary>
    /// Reports the outcome of a <c>ScaleWorkloadAsync</c> attempt back to the
    /// controller (AB#4917). The controller advances the workload's lifecycle
    /// state machine (AB#4914): a successful scale-to-0 ack completes the
    /// <c>Draining → Hibernated</c> transition; a failed scale surfaces on the
    /// workload's <c>StatusMessage</c> and as an audit event.
    /// </summary>
    Task ReportWorkloadScaleStatusAsync(WorkloadScaleStatusDto status);

    /// <summary>
    /// Registers a DeploymentSite the operator currently manages. The
    /// controller writes the deployment site's <c>CommunicationState</c> to
    /// <c>Online</c> and remembers the operator's SignalR connection id, so
    /// that when the connection drops every deployment site registered through it goes
    /// back to <c>Offline</c> automatically (via the hub's
    /// <c>OnDisconnectedAsync</c>).
    ///
    /// The (tenant, deploymentSiteRtId) tuple is the controller-side lookup key:
    /// stable across deployment site renames, DNS-safe, and what the operator uses
    /// for every derived Kubernetes resource (CR name, broker secret,
    /// release name). The human-readable deployment site display name lives on the
    /// controller's <c>RtDeploymentSite.Name</c> attribute and surfaces in Studio;
    /// it is not sent over the wire.
    ///
    /// Replaces the legacy per-pool <c>/poolHub</c> connection — each
    /// operator now keeps a single multiplexed <c>/operatorHub</c> channel
    /// regardless of how many deployment sites it owns.
    /// </summary>
    Task RegisterDeploymentSiteAsync(string tenantId, string deploymentSiteRtId);

    /// <summary>
    /// Unregisters a DeploymentSite. The controller flips the deployment site's
    /// <c>CommunicationState</c> to <c>Unregistered</c> and forgets the
    /// (connection, tenant, deploymentSiteRtId) tuple. Called by the operator when
    /// its <c>DeploymentSite</c> CR is deleted (graceful shutdown of
    /// one deployment site while the operator keeps running for others).
    /// </summary>
    Task UnregisterDeploymentSiteAsync(string tenantId, string deploymentSiteRtId);
}
