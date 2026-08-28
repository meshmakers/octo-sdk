namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Per-tenant on-demand lifecycle configuration (AB#4914), exposed via
///     <c>GET/PUT {tenantId}/v1/communication/lifecycle</c>. Runtime configuration — set per
///     tenant via octo-cli / Studio, no controller redeploy involved.
/// </summary>
/// <param name="ScaleToZeroEnabled">
///     Master switch for scale-to-zero on the tenant (default false). Even a workload with
///     <c>LifecycleMode=OnDemand</c> is never hibernated while this is off; switching it off is
///     the emergency stop (the idle watchdog stops hibernating, already-hibernated workloads
///     wake on next demand or via the wake API).
/// </param>
public record CommunicationLifecycleDto(bool ScaleToZeroEnabled);
