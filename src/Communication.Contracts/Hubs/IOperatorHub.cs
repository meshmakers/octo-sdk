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
    /// <returns>
    /// All currently-deployed Cloud pools across every tenant, so a freshly
    /// (re)connected operator can synchronize its desired state without
    /// missing pools that were deployed while it was offline.
    /// </returns>
    Task<IEnumerable<DeployedPoolDto>> RegisterOperatorAsync();

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
}
