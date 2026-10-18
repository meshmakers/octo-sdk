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
public sealed record RollupConsistencyEntryDto(
    string RollupRtId,
    bool IsOk,
    IReadOnlyList<RollupConsistencyFindingDto> Findings);

/// <summary>
///     The validator's result for a tenant: one entry per rollup that was checked.
/// </summary>
/// <param name="IsOk">True when every rollup is ok (also when there is none).</param>
/// <param name="CheckedAtUtc">When the check ran.</param>
/// <param name="Entries">One entry per checked rollup.</param>
public sealed record RollupConsistencyReportDto(
    bool IsOk,
    DateTime CheckedAtUtc,
    IReadOnlyList<RollupConsistencyEntryDto> Entries);

/// <summary>
///     One step of a rollup repair.
/// </summary>
/// <param name="RollupRtId">Runtime id of the rollup archive.</param>
/// <param name="Kind">
///     <c>RewindWatermark</c>, <c>RecomputePointerRange</c>, <c>DeleteOrphans</c> or <c>CreateGenerationMap</c>.
/// </param>
/// <param name="Description">English text of the step.</param>
/// <param name="Status">
///     <c>Planned</c> (dry run), <c>Skipped</c> (an earlier step of the rollup failed), and the states of a real run
///     (<c>Done</c>, <c>Failed</c>, <c>Enqueued</c>). A string, so a state this client does not know still deserializes.
/// </param>
/// <param name="RangeFromUtc">Inclusive start of the range concerned, when there is one.</param>
/// <param name="RangeToUtc">Exclusive end of the range concerned, when there is one.</param>
/// <param name="Generation">The generation concerned, when there is one.</param>
/// <param name="Error">The failure text of a failed step.</param>
/// <param name="JobId">The recompute job of an enqueued step, to be followed with the recompute job listing.</param>
public sealed record RollupRepairActionDto(
    string RollupRtId,
    string Kind,
    string Description,
    string Status,
    DateTime? RangeFromUtc,
    DateTime? RangeToUtc,
    long? Generation,
    string? Error,
    string? JobId);

/// <summary>
///     What a rollup repair did or, on a dry run, would do.
/// </summary>
/// <param name="DryRun">True when nothing was changed.</param>
/// <param name="IsOk">True when nothing failed (and, for an applied repair that has finished, the tenant validates ok).</param>
/// <param name="Before">The validator's report before any step.</param>
/// <param name="Actions">The steps in the order they were (or would be) run.</param>
/// <param name="After">The validator's report after the steps; <c>null</c> on a dry run and while jobs are still running.</param>
/// <param name="FailedStep">The first failed step, named; <c>null</c> when none failed.</param>
/// <param name="JobIds">The recompute jobs an applied repair enqueued (HTTP 202); empty otherwise.</param>
public sealed record RollupRepairReportDto(
    bool DryRun,
    bool IsOk,
    RollupConsistencyReportDto Before,
    IReadOnlyList<RollupRepairActionDto> Actions,
    RollupConsistencyReportDto? After,
    string? FailedStep,
    IReadOnlyList<string> JobIds);
