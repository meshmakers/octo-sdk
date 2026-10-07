namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     The lease a pool member is still running when it registers again after a reconnect
///     (AB#5826). Sent on <c>IAdapterPoolHub.ResumePoolMemberAsync</c>.
/// </summary>
/// <remarks>
///     <para>
///         🔴 <b>Identifiers only — no secret travels back.</b> The borrower's client secret, database
///         password, invoker token, pipeline configuration and input stay on the member, exactly as
///         <see cref="LeaseResultDto" /> rules for the release. The controller does not need them: it
///         only has to know that the member is busy, with what, until when.
///     </para>
///     <para>
///         The controller does not take these values on trust. It adopts the lease only after the
///         persisted execution in <see cref="TenantId" /> proves that the work was leased from this
///         member's pool to this member id and is still running.
///     </para>
/// </remarks>
public record PoolMemberActiveLeaseDto
{
    /// <summary>The lease id, as handed out in <see cref="LeaseDto.LeaseId" />.</summary>
    public string LeaseId { get; init; } = string.Empty;

    /// <summary>The borrowing tenant (<see cref="LeaseDto.TenantId" />).</summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>The borrower's execution the lease serves (<see cref="LeaseDto.ExecutionId" />); empty for a hand-driven lease.</summary>
    public string ExecutionId { get; init; } = string.Empty;

    /// <summary>RtId of the borrower's leased adapter (<see cref="LeaseDto.AdapterRtId" />).</summary>
    public string AdapterRtId { get; init; } = string.Empty;

    /// <summary>CkTypeId of the borrower's leased adapter (<see cref="LeaseDto.AdapterCkTypeId" />).</summary>
    public string AdapterCkTypeId { get; init; } = string.Empty;

    /// <summary>When the lease was granted (UTC), as handed out in <see cref="LeaseDto.GrantedAtUtc" />.</summary>
    public DateTime GrantedAtUtc { get; init; }

    /// <summary>
    ///     When the lease expires (UTC), as handed out in <see cref="LeaseDto.ExpiresAtUtc" />. An
    ///     adopted lease keeps it, so the TTL a controller restart interrupted is the TTL that applies.
    /// </summary>
    public DateTime ExpiresAtUtc { get; init; }
}
