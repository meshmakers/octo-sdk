namespace Meshmakers.Octo.Sdk.ServiceClient.AiServices;

/// <summary>
///     Snapshot of a tenant's Anthropic credential lease as returned by the AI Adapter.
///     Token ciphertext never crosses the wire — only the status + expiry metadata —
///     so an operator who can read this DTO still cannot extract the subscription.
///     <see cref="Status" /> uses the same string values as the server-side
///     <c>RtTokenLeaseStatusEnum</c> (<c>Active</c>, <c>NoLease</c>, <c>Revoked</c>,
///     <c>RefreshFailed</c>); the client treats them as opaque to avoid coupling the
///     SDK release cadence to a server-side enum addition.
/// </summary>
public sealed class CredentialsStatusDto
{
    /// <summary>
    ///     Lifecycle state of the lease. <c>NoLease</c> is the initial state before the
    ///     bastion CLI registers a subscription token — callers should treat that as
    ///     "register required" rather than an error.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>UTC instant the access token expires, or null when no lease exists.</summary>
    public DateTime? AccessExpiresAt { get; set; }

    /// <summary>UTC instant the refresh token expires, or null when no lease exists.</summary>
    public DateTime? RefreshExpiresAt { get; set; }

    /// <summary>
    ///     Monotonic counter that bumps on every register / refresh. The refresh worker
    ///     uses this as an optimistic-concurrency check; clients can surface it for
    ///     audit-trail correlation.
    /// </summary>
    public long Generation { get; set; }
}
