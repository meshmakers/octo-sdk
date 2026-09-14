namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     A pool member introducing itself on <c>/adapterPoolHub</c> (AB#4924, concept §4).
/// </summary>
/// <remarks>
///     <para>
///         🔴 <b>This connection is not tenant-addressed, and that is the whole point.</b> A pool
///         member belongs to no tenant; it is handed one per lease. The declared
///         <see cref="PoolTenantId" /> is the <b>lending</b> tenant — the owner of the pool — which
///         is what the connection is authorized against (concept §8, Q4). Authority over a
///         <i>borrower</i> comes from the lease, never from this connection.
///     </para>
///     <para>
///         <see cref="MemberId" /> is proposed by the member rather than assigned by the controller
///         because it has to survive a reconnect: it is what a borrower's execution records as
///         <c>LeasedOnMemberId</c>, and a value that changed on every network blip would make the
///         per-member queue view (concept §5) unreadable. A pod name is the natural value.
///     </para>
/// </remarks>
public record PoolMemberRegistrationDto
{
    /// <summary>
    ///     The tenant that owns the <c>AdapterPool</c> this member belongs to — the <b>lender</b>.
    ///     The hub refuses a registration whose declared tenant is not the one the connection's token
    ///     was issued for.
    /// </summary>
    public string PoolTenantId { get; init; } = string.Empty;

    /// <summary>
    ///     RtId of the <c>AdapterPool</c> in <see cref="PoolTenantId" />. Bare 24-character hex.
    /// </summary>
    public string PoolRtId { get; init; } = string.Empty;

    /// <summary>
    ///     Stable identity of this member process across reconnects. Recorded on every execution the
    ///     member serves as <c>LeasedOnMemberId</c>.
    /// </summary>
    public string MemberId { get; init; } = string.Empty;

    /// <summary>
    ///     Pipeline node names this member can execute. The scheduler (increment 7) matches queued
    ///     work against it; increment 6 only records it, so a member that reports none is still
    ///     leasable by hand.
    /// </summary>
    public IReadOnlyList<string> NodeNames { get; init; } = [];
}
