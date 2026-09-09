namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.StreamData;

/// <summary>
/// SDK projection of one rollup archive returned by
/// <see cref="IStreamDataServicesClient.ListRollupsForArchiveAsync"/>. Mirrors the asset-repo
/// REST payload exactly so the SDK layer is a thin transport. Rollup-archives concept §9.
/// </summary>
/// <param name="RtId">Runtime id of the rollup archive (string form of <c>OctoObjectId</c>).</param>
/// <param name="RtWellKnownName">Optional well-known name.</param>
/// <param name="Status">Lifecycle status — <c>Created</c> / <c>Activated</c> / <c>Disabled</c> / <c>Failed</c>.</param>
/// <param name="SourceArchiveRtId">
/// Runtime id of the single source archive this rollup aggregates from. Deprecated since
/// <c>System.StreamData</c> 1.8.0 (AB#5157): the server populates it only when the rollup has
/// exactly one source and that source is unbounded; it is <c>null</c> for every multi-source or
/// span-bounded rollup. Read <paramref name="Sources"/> instead.
/// </param>
/// <param name="BucketSizeMs">Bucket width in milliseconds.</param>
/// <param name="WatermarkLagMs">Watermark lag in milliseconds.</param>
/// <param name="LastAggregatedBucketEnd">Exclusive end timestamp of the most recently committed bucket; null before the first tick.</param>
/// <param name="FrozenUntil">Upper bound of the frozen range; null when not frozen.</param>
/// <param name="AggregationCount">Number of aggregation specs configured on this rollup.</param>
/// <param name="RecomputeInProgress">True while a recompute job for this rollup is running or swapping (AB#4184).</param>
/// <param name="LastRecomputeStartedAt">Start timestamp of the most recent recompute run; null before the first run.</param>
/// <param name="LastRecomputeSuccessAt">Finish timestamp of the most recent successfully committed recompute run; null before the first success.</param>
/// <param name="LastRecomputeFailureAt">Timestamp of the most recent failed recompute run; null if the last run succeeded.</param>
/// <param name="LastRecomputeFailureReason">Human-readable reason for the most recent recompute failure; null if the last run succeeded.</param>
/// <param name="DirtyWindowsPending">Number of dirty windows recorded on this archive (retroactive changes not yet propagated); 0 in the steady state.</param>
/// <param name="PendingRecomputeRanges">Number of pending recompute ranges queued on this archive (the recompute work list still to drain); 0 in the steady state.</param>
/// <param name="Sources">
/// The normalised, time-disjoint source archives this rollup aggregates from (AB#5157).
/// <c>null</c> when the server predates <c>System.StreamData</c> 1.8.0 and reported only
/// <paramref name="SourceArchiveRtId"/>.
/// </param>
public sealed record RollupArchiveInfoDto(
    string RtId,
    string? RtWellKnownName,
    string Status,
    string? SourceArchiveRtId,
    long BucketSizeMs,
    long WatermarkLagMs,
    DateTime? LastAggregatedBucketEnd,
    DateTime? FrozenUntil,
    int AggregationCount,
    bool RecomputeInProgress,
    DateTime? LastRecomputeStartedAt,
    DateTime? LastRecomputeSuccessAt,
    DateTime? LastRecomputeFailureAt,
    string? LastRecomputeFailureReason,
    int DirtyWindowsPending,
    int PendingRecomputeRanges,
    IReadOnlyList<RollupSourceReferenceDto>? Sources = null);
