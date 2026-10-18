// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Represents a job for data transfer.
/// </summary>
public class JobDto
{
    /// <summary>
    ///     Gets or sets the job id
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    ///     Datetime of job creation
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    ///     Datetime of last state change
    /// </summary>
    public DateTime? StateChangedAt { get; set; }

    /// <summary>
    ///     Current status
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    ///     Reason of the current state
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the error message if the job failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    ///     Result of a finished job: <c>Succeeded</c> or <c>SucceededWithWarnings</c> (the job status stays <c>Succeeded</c>,
    ///     but archives failed or rollups could not be checked in a way that matters, AB#6969). <c>null</c> when the bot does
    ///     not report an outcome (an older bot, or a job kind without one); a string, so a new value still deserializes.
    /// </summary>
    public string? Outcome { get; set; }

    /// <summary>
    ///     Archives that failed during a restore. Empty when none failed or the bot reports none.
    /// </summary>
    public IReadOnlyList<JobFailedArchiveDto>? FailedArchives { get; set; }

    /// <summary>
    ///     Rollups that could not be checked after a restore (blocking and informational ones). Empty or <c>null</c> when
    ///     none or the bot reports none.
    /// </summary>
    public IReadOnlyList<JobNotCheckedRollupDto>? NotCheckedRollups { get; set; }

    /// <summary>
    ///     True when the job succeeded but the outcome needs attention: <see cref="Outcome" /> is
    ///     <c>SucceededWithWarnings</c>, an archive failed or a rollup is blocked. Derived, so it also works against a bot
    ///     that reports the lists without the outcome.
    /// </summary>
    public bool HasWarnings => string.Equals(Outcome, "SucceededWithWarnings", StringComparison.OrdinalIgnoreCase)
                               || FailedArchives is { Count: > 0 }
                               || NotCheckedRollups is { } rollups && rollups.Any(r => r.IsBlocking);
}

/// <summary>
///     An archive that failed during a restore (AB#6969).
/// </summary>
/// <param name="ArchiveRtId">Runtime id of the archive.</param>
/// <param name="Reason">English text of the failure.</param>
public sealed record JobFailedArchiveDto(string ArchiveRtId, string? Reason);

/// <summary>
///     A rollup that could not be checked after a restore (AB#6968, AB#6969).
/// </summary>
/// <param name="RollupRtId">Runtime id of the rollup archive.</param>
/// <param name="Cause">
///     <c>NoLiveTable</c>, <c>NeverActivated</c>, <c>SourceMissing</c> or <c>ArchiveFailed</c> (a string, so a new cause
///     still deserializes).
/// </param>
/// <param name="Reason">English text of the reason.</param>
/// <param name="IsBlocking">False for <c>NeverActivated</c> (informational); true otherwise. Absent reads as <c>true</c>.</param>
public sealed record JobNotCheckedRollupDto(string RollupRtId, string Cause, string? Reason, bool IsBlocking = true);