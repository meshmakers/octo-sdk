namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     One secret sweep run of a tenant (bot services <c>GET {tenantId}/v1/secrets/sweep-runs</c>, newest first,
///     the last 50 are kept per tenant; AB#5528/AB#5544). Counts only - never a value, never ciphertext.
/// </summary>
public class SecretSweepRunDto
{
    /// <summary>
    ///     Run id (the Hangfire job id); addresses the run's dump.
    /// </summary>
    public string RunId { get; set; } = string.Empty;

    /// <summary>
    ///     Mode: <c>Verify</c>, <c>Encrypt</c>, <c>Reprotect</c> or <c>CleanupUnreadable</c>.
    /// </summary>
    public SecretSweepModeDto Mode { get; set; }

    /// <summary>
    ///     What started the run.
    /// </summary>
    public SecretSweepTriggerDto Trigger { get; set; }

    /// <summary>
    ///     Outcome; <see cref="SecretSweepOutcomeDto.Running" /> while the run is in progress.
    /// </summary>
    public SecretSweepOutcomeDto Outcome { get; set; }

    /// <summary>
    ///     Why the run was skipped or failed, or a remark (as in the report); e.g.
    ///     <c>Interrupted (service restart)</c> for a run whose bot process ended while it was running. Never
    ///     contains a value; <c>null</c> while running or when there is nothing to say.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    ///     Start (UTC).
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    ///     End (UTC); <c>null</c> while running.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    ///     User name of whoever started the run, or <c>null</c> (recurring, restore).
    /// </summary>
    public string? TriggeredBy { get; set; }

    /// <summary>
    ///     Counts per stored form BEFORE the run: the forms as found by the run's own scan (for a post-restore run
    ///     its first step). For <see cref="SecretSweepModeDto.Verify" /> the same as <see cref="TotalsAfter" />.
    /// </summary>
    public SecretFormCountsReportDto Totals { get; set; } = new();

    /// <summary>
    ///     Counts per stored form AFTER the run: the follow-up Verify of a writing run (Encrypt, Reprotect,
    ///     CleanupUnreadable, restore), the run's own scan for a Verify. <c>null</c> while running and when the run
    ///     was skipped or failed before that scan (AB#5539).
    /// </summary>
    public SecretFormCountsReportDto? TotalsAfter { get; set; }

    /// <summary>
    ///     Values written by this run (encrypted, re-protected, placeholders normalised, cleared); a value whose
    ///     write did not happen is not counted.
    /// </summary>
    public long ValuesRewritten { get; set; }

    /// <summary>
    ///     Values converted from legacy clear text or <c>enc:v1</c> to <c>enc:v2</c> by this run (included in
    ///     <see cref="ValuesRewritten" />).
    /// </summary>
    public long EncryptedCount { get; set; }

    /// <summary>
    ///     Values left as stored because they changed while the run was working on them; a run with such values is
    ///     <see cref="SecretSweepOutcomeDto.CompletedWithFailures" /> - run it again.
    /// </summary>
    public long SkippedConcurrentlyModified { get; set; }

    /// <summary>
    ///     Legacy clear-text placeholders converted once to "not set" (migration only).
    /// </summary>
    public long PlaceholdersNormalized { get; set; }

    /// <summary>
    ///     <see cref="SecretSweepModeDto.CleanupUnreadable" /> only: legacy <c>enc:v1</c> values kept although
    ///     unreadable, because only the legacy key is missing (configuration gap, not key loss).
    /// </summary>
    public long SkippedLegacyV1KeyMissing { get; set; }

    /// <summary>
    ///     Stored values that cannot be read (unknown key id) - re-entry tasks.
    /// </summary>
    public long UnreadableCount { get; set; }

    /// <summary>
    ///     The pre-sweep dump; <c>null</c> for <c>Verify</c> (no dump).
    /// </summary>
    public SecretSweepDumpDto? Dump { get; set; }
}

/// <summary>
///     Pre-sweep dump of a secret sweep run. Dumps are never downloadable; they can only be deleted early
///     (<c>DELETE {tenantId}/v1/secrets/sweep-runs/{runId}/dump</c>, role <c>SecretManagement</c>).
/// </summary>
public class SecretSweepDumpDto
{
    /// <summary>
    ///     File name on the bot service.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    ///     True while the file exists.
    /// </summary>
    public bool Exists { get; set; }

    /// <summary>
    ///     Size in bytes, or <c>null</c> when unknown.
    /// </summary>
    public long? SizeBytes { get; set; }

    /// <summary>
    ///     When the dump was taken (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    ///     When the dump expires (UTC); <see cref="CreatedAt" /> plus 7 days.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    ///     When the dump was deleted early or expired (UTC), or <c>null</c>.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    ///     User name of whoever deleted it early, or <c>null</c>.
    /// </summary>
    public string? DeletedBy { get; set; }
}

/// <summary>
///     Result of <c>DeleteSecretSweepDumpAsync</c>.
/// </summary>
public enum SecretSweepDumpDeleteResultDto
{
    /// <summary>
    ///     <c>204</c>: the dump was deleted.
    /// </summary>
    Deleted = 0,

    /// <summary>
    ///     <c>404</c>: unknown run, or the run has no dump (e.g. Verify).
    /// </summary>
    NotFound = 1,

    /// <summary>
    ///     <c>409</c>: the dump was already deleted (early or expired).
    /// </summary>
    AlreadyDeleted = 2
}
