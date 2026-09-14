namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     A pool member handing a lease back to the controller (AB#4924, concept §4).
/// </summary>
/// <remarks>
///     <para>
///         🔴 <b>The release is what ends the tenant's presence in the process.</b> By the time this
///         message is sent the member has already left the lease scope, cleared its token holder and
///         unloaded the borrower's CK model. The message is the report, not the trigger — a member
///         that reported a release but kept the tenant loaded would have the invariant backwards.
///     </para>
///     <para>
///         It carries no secret and no tenant payload, only the identity of the lease and what
///         happened to the work item. The borrower's credential travelled one way, on
///         <see cref="LeaseDto" />, and must not come back.
///     </para>
/// </remarks>
public record LeaseResultDto
{
    /// <summary>
    ///     The lease being released, as handed out in <see cref="LeaseDto.LeaseId" />. A release
    ///     naming a lease the controller no longer holds is stale and is ignored — it would otherwise
    ///     release whatever lease the member was given in the meantime.
    /// </summary>
    public string LeaseId { get; init; } = string.Empty;

    /// <summary>Why the lease ended.</summary>
    public LeaseReleaseReasonDto Reason { get; init; }

    /// <summary>
    ///     Whether the work item succeeded. Independent of <see cref="Reason" />: a
    ///     <see cref="LeaseReleaseReasonDto.Drained" /> release can still carry a completed work item.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    ///     Free-form human-readable outcome. Must never carry credential material — this string is
    ///     logged and stored on the execution.
    /// </summary>
    public string? StatusMessage { get; init; }

    /// <summary>
    ///     What <c>SetPipelineExecutionResult@1</c> wrote for this execution, or null when the pipeline
    ///     produced no result (AB#4924 §9.9 / D4).
    /// </summary>
    /// <remarks>
    ///     🔴 <b>The release is the only way a leased execution's output can reach its entity.</b> A
    ///     dedicated adapter reports it on <c>IAdapterHub.ReportExecutionEndAsync</c>, whose tenant and
    ///     adapter come from the <i>connection</i> — a pool member has no such connection, it holds a
    ///     tenant-free management channel. Without this field the borrower's execution would complete
    ///     with an empty <c>OutputData</c> where a dedicated adapter would have filled it, which is a
    ///     difference in behaviour nobody asked for.
    ///     <para>
    ///         This is pipeline output, not credential material — the rule that the lease's credential
    ///         travels one way only is unchanged.
    ///     </para>
    /// </remarks>
    public string? OutputData { get; init; }

    /// <summary>
    ///     How long the member actually spent running the work item, in milliseconds, or null when
    ///     it never got that far (AB#4924 increment 9).
    /// </summary>
    /// <remarks>
    ///     🔴 <b>The controller cannot measure this, and that is why it travels.</b> Concept §2.3
    ///     keeps the lease-held span and the pipeline-run span deliberately apart because the
    ///     difference between them <i>is</i> the per-lease warm-up — token, tenant repository, CK
    ///     model, pipeline registration — that a pool exists to amortise. But for a leased execution
    ///     the controller is what stamps <c>StartedAt</c>, at claim time, so the entity's own span is
    ///     identical to the held span by construction and the difference would read as zero forever.
    ///     The member is the only party that knows when the work really began.
    ///     <para>
    ///         Null on a lease that failed before the work item ran, and on a member built before
    ///         this field existed. The controller records no work or overhead sample in that case
    ///         rather than a fabricated zero, which would make the pool look infinitely wasteful.
    ///     </para>
    /// </remarks>
    public long? WorkDurationMs { get; init; }

    /// <summary>
    ///     When the member finished with the tenant (UTC). The controller stamps
    ///     <c>LeaseReleasedAt</c> from its own clock rather than this value; it is carried for
    ///     diagnostics of clock skew between controller and member.
    /// </summary>
    public DateTime ReleasedAtUtc { get; init; }
}
