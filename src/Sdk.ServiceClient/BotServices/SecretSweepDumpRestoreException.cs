using System.Net;

namespace Meshmakers.Octo.Sdk.ServiceClient.BotServices;

/// <summary>
///     Why bot services refused to restore a pre-sweep dump (<see cref="SecretSweepDumpRestoreException.Reason" />,
///     AB#5559).
/// </summary>
public enum SecretSweepDumpRestoreFailure
{
    /// <summary>
    ///     <c>400 ConfirmationRequired</c>: the request was sent without <c>confirm=true</c>.
    /// </summary>
    ConfirmationRequired = 0,

    /// <summary>
    ///     <c>404</c>: unknown run, the run has no pre-sweep dump, or the dump is no longer in the store.
    /// </summary>
    NotFound = 1,

    /// <summary>
    ///     <c>409 DumpDeleted</c>: the dump was deleted, early or because it expired.
    /// </summary>
    DumpDeleted = 2,

    /// <summary>
    ///     <c>409 DumpKeyMissing</c>: the dump is encrypted with a key id that is not in the key ring.
    /// </summary>
    DumpKeyMissing = 3
}

/// <summary>
///     Thrown by <see cref="IBotServicesClient.RestoreSecretSweepDumpAsync" /> when bot services refuses the restore
///     with <c>400</c>, <c>404</c> or <c>409</c> (AB#5559). The message is fixed per <see cref="Reason" /> and never
///     contains data from the response body.
/// </summary>
[Serializable]
public class SecretSweepDumpRestoreException : ServiceClientResultException
{
    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="reason">Why the restore was refused.</param>
    /// <param name="httpStatusCode">The HTTP status code received.</param>
    public SecretSweepDumpRestoreException(SecretSweepDumpRestoreFailure reason, HttpStatusCode httpStatusCode)
        : base(DescribeReason(reason), httpStatusCode)
    {
        Reason = reason;
    }

    /// <summary>
    ///     Why the restore was refused.
    /// </summary>
    public SecretSweepDumpRestoreFailure Reason { get; }

    private static string DescribeReason(SecretSweepDumpRestoreFailure reason)
    {
        return reason switch
        {
            SecretSweepDumpRestoreFailure.ConfirmationRequired =>
                "Restoring a pre-sweep dump replaces the tenant's data and must be confirmed (confirm=true).",
            SecretSweepDumpRestoreFailure.NotFound =>
                "No pre-sweep dump found for this run: unknown run, the run has no dump, or the dump is no longer stored.",
            SecretSweepDumpRestoreFailure.DumpDeleted =>
                "The pre-sweep dump of this run was deleted (early or expired).",
            SecretSweepDumpRestoreFailure.DumpKeyMissing =>
                "The pre-sweep dump is encrypted with a key id that is not in the key ring; add the key back to restore it.",
            _ => "The pre-sweep dump cannot be restored."
        };
    }
}
