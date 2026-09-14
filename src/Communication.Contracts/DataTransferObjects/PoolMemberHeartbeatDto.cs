namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Liveness of a pool member and of the lease it currently holds (AB#4924, concept §6).
/// </summary>
/// <remarks>
///     A SignalR connection can stay up while the process behind it is wedged, and concept §6's
///     "release never arrives" row is exactly that case. The heartbeat is what lets the controller
///     tell a long-running work item from a dead one without waiting for the transport to notice.
/// </remarks>
public record PoolMemberHeartbeatDto
{
    /// <summary>The member reporting, as accepted at registration.</summary>
    public string MemberId { get; init; } = string.Empty;

    /// <summary>
    ///     The lease the member believes it holds, or empty when it is idle. A heartbeat naming a
    ///     lease the controller has already expired is the signal that the two disagree.
    /// </summary>
    public string? ActiveLeaseId { get; init; }

    /// <summary>When the member sampled this report (UTC).</summary>
    public DateTime SampledAtUtc { get; init; }
}
