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
///     <para>
///         🔴 <b><see cref="NodeDescriptors" /> replaced an earlier <c>NodeNames</c> string list.</b>
///         A bare name cannot answer any of the questions the borrower's deploy path asks: the
///         execution class of a trigger, whether a trigger is process-bound, which node version is
///         present, or what the configuration schema of a node looks like. The member already builds
///         full descriptors for the dedicated registration path, so sending them here is reuse, not
///         a second source. The old field was never populated by any member and never read by any
///         controller, so nothing on the wire carried it in either direction.
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
    ///     The pipeline nodes this member can execute, in exactly the shape a dedicated adapter
    ///     reports on <c>RegisterAdapterWithSchemaAsync</c>.
    /// </summary>
    /// <remarks>
    ///     The controller stores them per pool so a <b>borrowing</b> tenant's deploy path can resolve
    ///     an execution class and classify process-bound triggers against the process that will
    ///     actually run the pipeline. A member that reports none degrades to the controller's
    ///     name-based fallback — the same degradation a dedicated adapter that never connected
    ///     produces — and stays leasable.
    /// </remarks>
    public IReadOnlyList<NodeDescriptorDto> NodeDescriptors { get; init; } = [];

    /// <summary>
    ///     The composite pipeline JSON Schema this member validates against, or null when it cannot
    ///     produce one. Same value a dedicated adapter sends on registration; the controller
    ///     validates a <b>leased</b> pipeline definition against it at deploy time, because
    ///     validating against no schema at all is worse than validating against the pool's.
    /// </summary>
    public string? PipelineSchemaJson { get; init; }
}
