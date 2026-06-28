namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.StreamData;

/// <summary>
/// SDK projection of a recompute job snapshot returned by
/// <see cref="IStreamDataServicesClient.RecomputeArchiveAsync"/> or
/// <see cref="IStreamDataServicesClient.ListRecomputeJobsForArchiveAsync"/>. Mirrors the asset-repo
/// REST payload exactly so the SDK layer stays a thin transport (AB#4184).
/// </summary>
/// <param name="RtId">Runtime id of the recompute job (string form of <c>OctoObjectId</c>).</param>
/// <param name="State">Job lifecycle state — <c>Pending</c> / <c>Running</c> / <c>Swapping</c> / <c>Completed</c> / <c>Failed</c> / <c>Coalesced</c>.</param>
/// <param name="RowsProcessed">Rows written into the staging table; null while pending.</param>
/// <param name="WindowsProcessed">Buckets recomputed; null while pending.</param>
/// <param name="StartedAt">When compute started; null while pending.</param>
/// <param name="FinishedAt">When the job reached a terminal state; null while running.</param>
/// <param name="DurationMs">Wall-clock duration in milliseconds; null while running.</param>
/// <param name="ErrorReason">Failure reason when <see cref="State"/> is <c>Failed</c>; null otherwise.</param>
public sealed record RollupRecomputeJobInfoDto(
    string RtId,
    string State,
    int? RowsProcessed,
    int? WindowsProcessed,
    DateTime? StartedAt,
    DateTime? FinishedAt,
    int? DurationMs,
    string? ErrorReason);
