namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.StreamData;

/// <summary>
/// SDK projection of one rollup archive returned by
/// <see cref="IStreamDataServicesClient.ListRollupsForArchiveAsync"/>. Mirrors the asset-repo
/// REST payload exactly so the SDK layer is a thin transport. Rollup-archives concept §9.
/// </summary>
/// <param name="RtId">Runtime id of the rollup archive (string form of <c>OctoObjectId</c>).</param>
/// <param name="RtWellKnownName">Optional well-known name.</param>
/// <param name="Status">Lifecycle status — <c>Created</c> / <c>Activated</c> / <c>Disabled</c> / <c>Failed</c>.</param>
/// <param name="SourceArchiveRtId">Runtime id of the source archive this rollup aggregates from.</param>
/// <param name="BucketSizeMs">Bucket width in milliseconds.</param>
/// <param name="WatermarkLagMs">Watermark lag in milliseconds.</param>
/// <param name="LastAggregatedBucketEnd">Exclusive end timestamp of the most recently committed bucket; null before the first tick.</param>
/// <param name="FrozenUntil">Upper bound of the frozen range; null when not frozen.</param>
/// <param name="AggregationCount">Number of aggregation specs configured on this rollup.</param>
public sealed record RollupArchiveInfoDto(
    string RtId,
    string? RtWellKnownName,
    string Status,
    string SourceArchiveRtId,
    long BucketSizeMs,
    long WatermarkLagMs,
    DateTime? LastAggregatedBucketEnd,
    DateTime? FrozenUntil,
    int AggregationCount);
