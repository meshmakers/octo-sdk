namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Per-tenant communication lifecycle configuration (AB#4914), exposed via
///     <c>GET/PUT {tenantId}/v1/communication/lifecycle</c>. Runtime configuration — set per
///     tenant via octo-cli / Studio, no controller redeploy involved.
/// </summary>
/// <param name="ScaleToZeroEnabled">
///     Master switch for scale-to-zero on the tenant (default false). Even a workload with
///     <c>LifecycleMode=OnDemand</c> is never hibernated while this is off; switching it off is
///     the emergency stop (the idle watchdog stops hibernating, already-hibernated workloads
///     wake on next demand or via the wake API).
/// </param>
/// <param name="LeasingEnabled">
///     Master switch for adapter-pool leasing on the tenant (AB#4924, default false). It governs
///     <b>both</b> halves of a lease and the tenant means a different thing in each, which is why one
///     flag is enough: on the <b>lending</b> tenant it is "this tenant's pools hand their members
///     out", and on the <b>borrowing</b> tenant it is "this tenant's Leased adapters get scheduled".
///     A lease needs both to be on — lending is the lender's capability, borrowing is the borrower's,
///     and neither tenant can assert the other's (concept §13).
///     <para>
///         🔴 Switching it off <b>holds</b> the queue; it neither drains it nor cancels it. Work
///         already <c>Queued</c> stays <c>Queued</c> and visible in all three queue surfaces, no new
///         work is enqueued and no lease is granted. Draining would mean "off" still runs the next
///         hour of work; cancelling would destroy work an operator never asked to lose. Turning the
///         switch back on resumes the queue in its original order; <c>DELETE
///         {tenantId}/v1/adapterPool/{id}/queue/{executionId}</c> is how an operator discards it.
///     </para>
/// </param>
public record CommunicationLifecycleDto(bool ScaleToZeroEnabled, bool LeasingEnabled = false);
