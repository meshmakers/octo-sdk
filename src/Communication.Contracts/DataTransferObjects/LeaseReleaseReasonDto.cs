namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Why a pool member handed a lease back (AB#4924, concept §4 and §6).
/// </summary>
/// <remarks>
///     Distinguishing the reasons is what lets the controller decide between "re-use this member"
///     and "drain and restart it". A member that completed or failed a work item cleanly has proven
///     its own cleanliness; one that was drained mid-flight has not.
/// </remarks>
public enum LeaseReleaseReasonDto
{
    /// <summary>The work item finished. The member is clean and may take the next lease.</summary>
    Completed = 0,

    /// <summary>
    ///     The work item failed. Still a clean release — the member unwound the lease itself, so the
    ///     failure belongs to the pipeline, not to the isolation invariant.
    /// </summary>
    Failed = 1,

    /// <summary>
    ///     The member is shutting down or was asked to drain while it held the lease. The controller
    ///     re-queues the work (at-least-once, concept §6) and does not hand this member another lease.
    /// </summary>
    Drained = 2
}
