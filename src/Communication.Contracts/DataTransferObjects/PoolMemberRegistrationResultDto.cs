namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     The controller's answer to <c>IAdapterPoolHub.RegisterPoolMemberAsync</c> (AB#4924).
/// </summary>
public record PoolMemberRegistrationResultDto
{
    /// <summary>
    ///     Whether the member is now eligible for leases. A rejected registration keeps the
    ///     connection open — the member logs and retries — rather than aborting it, so a pool that is
    ///     momentarily unresolvable (mid tenant update) does not turn into a crash loop.
    /// </summary>
    public bool Accepted { get; init; }

    /// <summary>
    ///     The member id the controller will use. Echoes <see cref="PoolMemberRegistrationDto.MemberId" />
    ///     when it was usable and substitutes the connection id when it was blank, so the member can
    ///     log the value its executions will be attributed to.
    /// </summary>
    public string MemberId { get; init; } = string.Empty;

    /// <summary>
    ///     How often the member should call <c>HeartbeatAsync</c>, in seconds. The controller expires
    ///     a lease held by a member it has not heard from — concept §6, "release never arrives".
    /// </summary>
    public int HeartbeatIntervalSeconds { get; init; }

    /// <summary>
    ///     Why a registration was refused. Null on success — except on a resumption whose lease could
    ///     not be adopted, where it says why (AB#5826).
    /// </summary>
    public string? StatusMessage { get; init; }

    /// <summary>
    ///     On <c>ResumePoolMemberAsync</c> only (AB#5826): whether the controller took over the lease the
    ///     member reported, so the member's release will complete the borrower's execution. False when
    ///     the execution has moved on (the controller already interrupted and re-queued it, or it is
    ///     terminal) — the member is then still recorded as busy until it releases, but its outcome is
    ///     not applied. Always false on a plain registration.
    /// </summary>
    public bool ActiveLeaseAdopted { get; init; }
}
