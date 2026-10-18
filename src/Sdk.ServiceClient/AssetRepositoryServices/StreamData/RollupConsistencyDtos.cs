namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.StreamData;

/// <summary>
///     One thing the rollup validator found wrong with a rollup (AB#6968). Mirrors the asset-repo REST payload.
/// </summary>
/// <param name="Check">
///     Which check failed: <c>PointerWithoutRows</c>, <c>OrphansAbovePointer</c>, <c>WatermarkAheadOfRows</c> or
///     <c>MissingGenerationMap</c>. A string so a value this client does not know deserializes instead of throwing.
/// </param>
/// <param name="Message">English text for logs and reports.</param>
/// <param name="RangeFromUtc">Inclusive start of the range concerned, when there is one.</param>
/// <param name="RangeToUtc">Exclusive end of the range concerned, when there is one.</param>
/// <param name="Generation">The generation concerned.</param>
/// <param name="Rows">Rows concerned.</param>
/// <param name="Gap">For <c>WatermarkAheadOfRows</c>: the gap between the watermark and the last rollup row (time span text).</param>
public sealed record RollupConsistencyFindingDto(
    string Check,
    string Message,
    DateTime? RangeFromUtc,
    DateTime? RangeToUtc,
    long? Generation,
    long? Rows,
    string? Gap);

/// <summary>
///     The validator's result for one rollup.
/// </summary>
/// <param name="RollupRtId">Runtime id of the rollup archive.</param>
/// <param name="IsOk">True when no check found anything.</param>
/// <param name="Findings">What was found; empty when <paramref name="IsOk" />.</param>
/// <param name="IsNotChecked">
///     True when the rollup could not be checked (see <paramref name="NotChecked" />). Such an entry is not ok unless the gap is
///     informational (<c>NotChecked.IsBlocking</c> false). Against an older server this reads as <c>false</c>.
/// </param>
/// <param name="NotChecked">Why the rollup could not be checked; <c>null</c> when every check ran.</param>
/// <param name="IsBlockedByNotChecked">True when the gap is blocking, which makes <paramref name="IsOk" /> false.</param>
public sealed record RollupConsistencyEntryDto(
    string RollupRtId,
    bool IsOk,
    IReadOnlyList<RollupConsistencyFindingDto> Findings,
    bool IsNotChecked = false,
    RollupNotCheckedDto? NotChecked = null,
    bool IsBlockedByNotChecked = false);

/// <summary>
///     Why the validator could not check a rollup.
/// </summary>
/// <param name="Cause">
///     <c>NoLiveTable</c>, <c>NeverActivated</c> or <c>SourceMissing</c> (a string, so a new cause still deserializes).
/// </param>
/// <param name="Reason">English text of the reason.</param>
/// <param name="IsBlocking">
///     False for <c>NeverActivated</c> (informational: the rollup has no data, the entry and report stay ok), true otherwise.
///     Absent on an older server, which reads as <c>true</c>.
/// </param>
public sealed record RollupNotCheckedDto(string Cause, string Reason, bool IsBlocking = true);

/// <summary>
///     The validator's result for a tenant: one entry per rollup that was checked.
/// </summary>
/// <param name="IsOk">True when every rollup is ok (also when there is none).</param>
/// <param name="CheckedAtUtc">When the check ran.</param>
/// <param name="Entries">One entry per checked rollup.</param>
/// <param name="HasNotChecked">True when at least one rollup could not be checked, informational entries included.</param>
/// <param name="HasBlockingNotChecked">True when at least one rollup could not be checked in a way that makes <paramref name="IsOk" /> false.</param>
public sealed record RollupConsistencyReportDto(
    bool IsOk,
    DateTime CheckedAtUtc,
    IReadOnlyList<RollupConsistencyEntryDto> Entries,
    bool HasNotChecked = false,
    bool HasBlockingNotChecked = false);

/// <summary>
///     One step of a rollup repair.
/// </summary>
/// <param name="RollupRtId">Runtime id of the rollup archive.</param>
/// <param name="Kind">
///     <c>RewindWatermark</c>, <c>RecomputePointerRange</c>, <c>DeleteOrphans</c> or <c>CreateGenerationMap</c>.
/// </param>
/// <param name="Description">English text of the step.</param>
/// <param name="Status">
///     <c>Planned</c> (dry run), <c>Done</c>, <c>Failed</c> or <c>Skipped</c> (an earlier step of the rollup failed).
///     A string, so a state this client does not know still deserializes.
/// </param>
/// <param name="RangeFromUtc">Inclusive start of the range concerned, when there is one.</param>
/// <param name="RangeToUtc">Exclusive end of the range concerned, when there is one.</param>
/// <param name="Generation">The generation concerned, when there is one.</param>
/// <param name="Error">The failure text of a failed step.</param>
public sealed record RollupRepairActionDto(
    string RollupRtId,
    string Kind,
    string Description,
    string Status,
    DateTime? RangeFromUtc,
    DateTime? RangeToUtc,
    long? Generation,
    string? Error);

/// <summary>
///     What a rollup repair did or, on a dry run, would do.
/// </summary>
/// <param name="DryRun">True when nothing was changed.</param>
/// <param name="IsOk">True when nothing failed (and, for an applied repair, the tenant validates ok).</param>
/// <param name="Before">The validator's report before any step.</param>
/// <param name="Actions">The steps in the order they were (or would be) run.</param>
/// <param name="After">The validator's report after the steps; <c>null</c> on a dry run.</param>
/// <param name="FailedStep">The first failed step, named; <c>null</c> when none failed.</param>
public sealed record RollupRepairReportDto(
    bool DryRun,
    bool IsOk,
    RollupConsistencyReportDto Before,
    IReadOnlyList<RollupRepairActionDto> Actions,
    RollupConsistencyReportDto? After,
    string? FailedStep);

/// <summary>
///     An applied rollup repair (AB#6968). The request itself runs nothing: the server executes the repair in the
///     background, so a large recompute is never bound to an HTTP request. Follow it with
///     <see cref="IStreamDataServicesClient.GetRollupRepairRequestAsync" /> until <see cref="State" /> is
///     <c>Done</c> or <c>Failed</c>.
/// </summary>
/// <param name="RequestId">Id of the repair request; a second request while one is active returns the active one.</param>
/// <param name="State"><c>Pending</c>, <c>Running</c>, <c>Done</c> or <c>Failed</c> (a string, so a new state still deserializes).</param>
/// <param name="RequestedAtUtc">When the repair was requested.</param>
/// <param name="FinishedAtUtc">When it ended; <c>null</c> while it is pending or running.</param>
/// <param name="PlannedActions">The steps planned when the repair was requested.</param>
/// <param name="Report">The final report (steps, validation after, failed step); <c>null</c> until the repair has ended.</param>
public sealed record RollupRepairRequestDto(
    string RequestId,
    string State,
    DateTime RequestedAtUtc,
    DateTime? FinishedAtUtc,
    IReadOnlyList<RollupRepairActionDto> PlannedActions,
    RollupRepairReportDto? Report)
{
    /// <summary>True when the repair has ended, successfully or not.</summary>
    public bool IsFinished => State is "Done" or "Failed";
}
