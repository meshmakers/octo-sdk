namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     What happened when a queue entry was asked to be cancelled —
///     <c>DELETE {tenantId}/v1/adapterPool/{adapterPoolRtId}/queue/{executionId}</c> (AB#4924 §10).
/// </summary>
/// <remarks>
///     🔴 <b><see cref="AlreadyLeased" /> is a first-class outcome, not an error.</b> The server
///     answers <c>409 Conflict</c> for an execution that already holds a lease, because cancelling a
///     queued entry and interrupting a running pipeline are two different operations (concept §5,
///     "Cancellation"). Surfacing it as a generic failure would hide from the operator which of the
///     two they just failed to perform — so it travels as its own value and every surface says so.
/// </remarks>
public enum AdapterPoolQueueCancellationOutcome
{
    /// <summary>The entry was waiting and is now <c>Cancelled</c>. It will never be leased.</summary>
    Cancelled = 0,

    /// <summary>
    ///     The execution already holds a lease (<c>409</c>). Nothing was cancelled. Stopping it means
    ///     interrupting the running pipeline, which is the other operation.
    /// </summary>
    AlreadyLeased = 1,

    /// <summary>
    ///     No queued execution with that id belongs to this pool (<c>404</c>) — it never existed here,
    ///     or it already finished, failed or was cancelled.
    /// </summary>
    NotFound = 2
}

/// <summary>
///     Result of cancelling one adapter pool queue entry.
/// </summary>
public class AdapterPoolQueueCancellationResultDto
{
    /// <summary>What the server did.</summary>
    public AdapterPoolQueueCancellationOutcome Outcome { get; set; }

    /// <summary>The server's explanation, when it sent one. Empty on the success path.</summary>
    public string? ServerMessage { get; set; }

    /// <summary>Whether the entry really was cancelled.</summary>
    public bool IsCancelled => Outcome == AdapterPoolQueueCancellationOutcome.Cancelled;
}
