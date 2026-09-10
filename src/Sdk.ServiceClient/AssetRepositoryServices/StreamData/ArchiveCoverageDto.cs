namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.StreamData;

/// <summary>
/// SDK projection of the measured data coverage of one archive in a tenant's stream-data
/// landscape (AB#5157), returned by <see cref="IStreamDataServicesClient.GetArchiveCoverageAsync"/>.
/// Mirrors the asset-repo REST payload exactly so the SDK layer stays a thin transport; every
/// enum-valued member travels as its PascalCase name.
/// </summary>
/// <param name="ArchiveRtId">Runtime id of the archive (string form of <c>OctoObjectId</c>).</param>
/// <param name="RtWellKnownName">Optional well-known name of the archive.</param>
/// <param name="IsBase">True when the archive is not a rollup, i.e. it is written to directly.</param>
/// <param name="Status">Lifecycle status — <c>Created</c> / <c>Activated</c> / <c>Disabled</c> / <c>Failed</c>.</param>
/// <param name="BucketSizeMs">Bucket width in milliseconds; <c>null</c> for a raw base archive, which has no buckets.</param>
/// <param name="BucketAlignment">Bucket alignment as its enum name — <c>FixedSize</c> / <c>CalendarDay</c> / <c>Iso8601Week</c> / <c>CalendarMonth</c> / <c>CalendarQuarter</c> / <c>CalendarYear</c>; a base archive reports <c>FixedSize</c>.</param>
/// <param name="StoredFunctions">Aggregation functions the rollup declares, as enum names; empty (or <c>null</c>) for a base archive.</param>
/// <param name="AvailableFrom">Timestamp of the earliest stored row; <c>null</c> when the archive holds no data.</param>
/// <param name="AvailableTo">Timestamp of the latest stored row; <c>null</c> when the archive holds no data.</param>
public sealed record ArchiveCoverageDto(
    string ArchiveRtId,
    string? RtWellKnownName,
    bool IsBase,
    string Status,
    long? BucketSizeMs,
    string BucketAlignment,
    IReadOnlyList<string>? StoredFunctions,
    DateTime? AvailableFrom,
    DateTime? AvailableTo);
