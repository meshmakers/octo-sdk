using System.Text.Json.Serialization;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     One entry of an adapter pool's queue, as every surface reads it —
///     <c>GET {tenantId}/v1/adapterPool/{adapterPoolRtId}/queue</c> (AB#4924, increments 7 and 8).
/// </summary>
/// <remarks>
///     <para>
///         🔴 <b>There is no global rank field, and adding one would be a lie.</b> The pool serves
///         borrowing tenants round-robin, so the only truthful answer is the pair
///         <see cref="PositionInTenant" /> + <see cref="TenantsAheadInRotation" />: where this item
///         sits inside its own tenant's queue, and how many other tenants take a turn first. A single
///         number would contradict the order work actually runs in (concept §5 "Fairness",
///         implementation plan §9.2).
///     </para>
///     <para>
///         🔴 <b>Mirrors the controller's own <c>Models/AdapterPoolQueueEntryDto</c> one-to-one.</b>
///         It is a separate declaration rather than a shared one because the controller does not take
///         this package for its own response models; keep the two in step when either changes.
///     </para>
///     <para>
///         A GraphQL query cannot serve this shape: the two position numbers are the scheduler's
///         rotation cursor — process state, not entity state — and the entries span tenant databases,
///         the pool belonging to the lender and every execution to a borrower.
///     </para>
/// </remarks>
public class AdapterPoolQueueEntryDto
{
    /// <summary>The borrower's execution id.</summary>
    public string ExecutionId { get; set; } = string.Empty;

    /// <summary>The tenant whose work this is.</summary>
    public string BorrowerTenantId { get; set; } = string.Empty;

    /// <summary>RtId of the pipeline to run, when the association resolves.</summary>
    public string? PipelineRtId { get; set; }

    /// <summary>Display name of that pipeline.</summary>
    public string? PipelineName { get; set; }

    /// <summary>
    ///     <c>0</c> Interactive, <c>1</c> Batch. Interactive is served first <b>within this tenant's
    ///     turn</b> and has no effect at all across tenants. An <c>int</c> and not an enum for the same
    ///     reason as <see cref="NodeDescriptorDto.ExecutionClass" />: a value the reading side does not
    ///     know must deserialize rather than throw.
    /// </summary>
    public int ExecutionClass { get; set; }

    /// <summary>When the work item entered the queue.</summary>
    public DateTime QueuedAtUtc { get; set; }

    /// <summary>1-based position inside this tenant's own queue; <c>0</c> once the item is leased.</summary>
    public int PositionInTenant { get; set; }

    /// <summary>
    ///     How many other borrowing tenants with queued work are served before this tenant's next
    ///     turn; <c>0</c> once the item is leased.
    /// </summary>
    public int TenantsAheadInRotation { get; set; }

    /// <summary>
    ///     The pool member holding this item, or <c>null</c> while it waits. Nothing enforces
    ///     referential integrity on this value — it may name a process that no longer exists
    ///     (implementation plan §12.1) — so render it, never resolve it.
    /// </summary>
    public string? LeasedOnMemberId { get; set; }

    /// <summary>When the holding lease expires; <c>null</c> while the item waits.</summary>
    public DateTime? LeaseExpiresAtUtc { get; set; }

    /// <summary>
    ///     Whether this entry is already running on a pool member. A leased entry cannot be cancelled
    ///     through the queue verb — that would be interrupting a running pipeline, a different
    ///     operation (concept §5 "Cancellation").
    /// </summary>
    /// <remarks>
    ///     Derived, and kept off the wire: the controller serialises this same type, and a computed
    ///     member would add a field to the response that says nothing the caller cannot read from
    ///     <see cref="LeasedOnMemberId" />.
    /// </remarks>
    [JsonIgnore]
    public bool IsLeased => !string.IsNullOrEmpty(LeasedOnMemberId);
}
