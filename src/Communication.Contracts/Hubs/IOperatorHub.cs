using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

namespace Meshmakers.Octo.Communication.Contracts.Hubs;

/// <summary>
/// Server-side hub interface for operator management connections.
/// Used by the central Communication Operator to register for Cloud pool
/// deploy / undeploy notifications.
/// </summary>
public interface IOperatorHub
{
    /// <summary>
    /// Registers the operator for receiving Cloud pool deploy / undeploy
    /// events.
    /// </summary>
    /// <param name="autoManagePools">
    /// The calling operator's <c>AutoManagePools</c> setting:
    /// <c>true</c> = central operator (creates / deletes CRs in response to
    /// controller broadcasts), <c>false</c> = edge operator (CRs are managed
    /// out-of-band on the edge cluster). The controller stores this per
    /// connection and validates it against <c>RtPool.Environment</c> on every
    /// <see cref="RegisterPoolAsync"/> call so a Cloud pool cannot be claimed
    /// by an edge operator (and vice versa). <c>null</c> means the operator
    /// did not declare a mode (legacy build pre-dating this parameter); the
    /// controller logs and audit-records the registration but does not
    /// enforce mode/environment matching, so a rolling upgrade does not break
    /// existing connections.
    /// </param>
    /// <returns>
    /// All currently-deployed Cloud pools across every tenant, so a freshly
    /// (re)connected operator can synchronize its desired state without
    /// missing pools that were deployed while it was offline.
    /// </returns>
    Task<IEnumerable<DeployedPoolDto>> RegisterOperatorAsync(bool? autoManagePools = null);

    /// <summary>
    /// Unregisters the operator from receiving pool deploy / undeploy events.
    /// </summary>
    Task UnregisterOperatorAsync();

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
    /// Registers a CommunicationPool the operator currently manages. The
    /// controller writes the pool's <c>CommunicationState</c> to
    /// <c>Online</c> and remembers the operator's SignalR connection id, so
    /// that when the connection drops every pool registered through it goes
    /// back to <c>Offline</c> automatically (via the hub's
    /// <c>OnDisconnectedAsync</c>).
    ///
    /// The (tenant, poolRtId) tuple is the controller-side lookup key:
    /// stable across pool renames, DNS-safe, and what the operator uses
    /// for every derived Kubernetes resource (CR name, broker secret,
    /// release name). The human-readable pool display name lives on the
    /// controller's <c>RtPool.Name</c> attribute and surfaces in Studio;
    /// it is not sent over the wire.
    ///
    /// Replaces the legacy per-pool <c>/poolHub</c> connection — each
    /// operator now keeps a single multiplexed <c>/operatorHub</c> channel
    /// regardless of how many pools it owns.
    /// </summary>
    Task RegisterPoolAsync(string tenantId, string poolRtId);

    /// <summary>
    /// Unregisters a CommunicationPool. The controller flips the pool's
    /// <c>CommunicationState</c> to <c>Unregistered</c> and forgets the
    /// (connection, tenant, poolRtId) tuple. Called by the operator when
    /// its <c>CommunicationPool</c> CR is deleted (graceful shutdown of
    /// one pool while the operator keeps running for others).
    /// </summary>
    Task UnregisterPoolAsync(string tenantId, string poolRtId);
}
