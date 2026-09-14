using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Meshmakers.Octo.Communication.Contracts.Hubs;

/// <summary>
///     Server-side hub interface of the adapter <b>pool</b> management channel at
///     <c>/adapterPoolHub</c> (AB#4924, concept §4).
/// </summary>
/// <remarks>
///     <para>
///         🔴 <b>A separate hub from <see cref="IAdapterHub" />, deliberately.</b>
///         <c>/{tenantId:tenantId}/adapterHub</c> is tenant-addressed by construction and
///         <c>AdapterHubAuthorizationFilter</c> (AB#5063) exists precisely to bind such a connection
///         to its route tenant. A pool member belongs to <b>no</b> tenant — it is handed one per
///         lease — so it cannot use that route, and weakening that filter to let it would give away
///         the one check the adapter data plane has. The pool channel is mounted next to
///         <c>/operatorHub</c> with its own staged gate instead.
///     </para>
///     <para>
///         <b>What the connection proves and what it does not.</b> The connection is authorized
///         against the <b>lending</b> tenant's read-write policy (concept §8, Q4) and bound to that
///         tenant. It proves the member belongs to a pool that tenant owns. It proves nothing at all
///         about a borrower: the authority to act inside a borrowing tenant travels on
///         <see cref="LeaseDto" />, as that tenant's own <c>PipelineServiceAccount</c> credential, and
///         expires with the lease.
///     </para>
///     <para>
///         Skew rule, as for AB#4917: controller, <c>octo-sdk</c> and the adapter SDK ship together,
///         and both directions degrade through the once-only <c>HubException</c> pattern so a rolling
///         upgrade window logs once instead of flooding.
///     </para>
/// </remarks>
public interface IAdapterPoolHub
{
    /// <summary>
    ///     Registers the calling process as a member of an adapter pool, making it eligible for
    ///     leases.
    /// </summary>
    /// <remarks>
    ///     The declared <see cref="PoolMemberRegistrationDto.PoolTenantId" /> is checked against the
    ///     tenant the connection's token was issued for. Under the gate's <c>Enforce</c> mode a
    ///     mismatch — or a connection with no tenant at all — is refused with a <c>HubException</c>;
    ///     under <c>LogOnly</c> it is logged and allowed, exactly like the other two hub gates, so the
    ///     mode can be armed per environment without a release.
    /// </remarks>
    Task<PoolMemberRegistrationResultDto> RegisterPoolMemberAsync(PoolMemberRegistrationDto registration);

    /// <summary>
    ///     Hands a lease back after the work item is done (concept §4).
    /// </summary>
    /// <remarks>
    ///     🔴 Called <b>after</b> the member has already dropped everything tenant-scoped — the
    ///     message reports the release, it does not cause it. A release naming a lease the controller
    ///     no longer holds is ignored rather than applied to the member's current lease.
    /// </remarks>
    Task ReleaseLeaseAsync(LeaseResultDto result);

    /// <summary>
    ///     Reports that the member — and the lease it holds, if any — is still alive.
    /// </summary>
    /// <remarks>
    ///     A SignalR connection can stay up while the process behind it is wedged. This is what lets
    ///     the controller tell a long work item from a dead member without waiting for the transport,
    ///     and it is the input to concept §6's "release never arrives" row.
    /// </remarks>
    Task HeartbeatAsync(PoolMemberHeartbeatDto heartbeat);
}
