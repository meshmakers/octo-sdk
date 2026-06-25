using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.StreamData;

/// <summary>
///     Interface for the StreamData services.
/// </summary>
public interface IStreamDataServicesClient : IServiceClient
{
    /// <summary>
    ///     Enables the StreamData for a tenant
    /// </summary>
    /// <param name="tenantId">The id of the tenant.</param>
    Task EnableAsync(string tenantId);

    /// <summary>
    ///     Disables StreamData  for a tenant
    /// </summary>
    /// <param name="tenantId">The id of the tenant.</param>
    Task DisableAsync(string tenantId);

    /// <summary>
    ///     Activates a CkArchive: provisions the per-archive CrateDB table and transitions the
    ///     archive to <c>Activated</c>. Allowed from <c>Created</c>, <c>Disabled</c>, or
    ///     <c>Failed</c>; idempotent on <c>Activated</c>.
    /// </summary>
    /// <param name="tenantId">The tenant that owns the archive.</param>
    /// <param name="archiveRtId">Runtime id of the <c>CkArchive</c> entity.</param>
    Task ActivateArchiveAsync(string tenantId, string archiveRtId);

    /// <summary>
    ///     Disables a CkArchive: transitions to <c>Disabled</c> (data preserved). Allowed only
    ///     from <c>Activated</c>.
    /// </summary>
    Task DisableArchiveAsync(string tenantId, string archiveRtId);

    /// <summary>
    ///     Re-enables a previously disabled archive: transitions <c>Disabled → Activated</c>.
    ///     Re-validates column paths against the current CK model; no DDL.
    /// </summary>
    Task EnableArchiveAsync(string tenantId, string archiveRtId);

    /// <summary>
    ///     Retries activation after a previous DDL failure. Allowed only from <c>Failed</c>.
    /// </summary>
    Task RetryArchiveActivationAsync(string tenantId, string archiveRtId);

    /// <summary>
    ///     Drops the per-archive CrateDB table (idempotent) and soft-deletes the <c>CkArchive</c>
    ///     entity. Destructive — historical data is lost. Allowed from any status.
    /// </summary>
    Task DeleteArchiveAsync(string tenantId, string archiveRtId);

    // ---- Rollup-archive mutations (concept §9) ----

    /// <summary>
    ///     Freezes a <c>CkRollupArchive</c> at <paramref name="until"/>. Monotonic — rejected when
    ///     the new value is earlier than the current FrozenUntil.
    /// </summary>
    Task FreezeRollupArchiveAsync(string tenantId, string rollupRtId, DateTime until);

    /// <summary>
    ///     Clears FrozenUntil on the rollup archive. Idempotent. Set <paramref name="acceptGaps"/>
    ///     when source data inside the previously frozen range has been truncated and the operator
    ///     knowingly accepts the resulting gaps.
    /// </summary>
    Task UnfreezeRollupArchiveAsync(string tenantId, string rollupRtId, bool acceptGaps = false);

    /// <summary>
    ///     Resets the rollup's watermark (truncated to the bucket boundary) so subsequent
    ///     orchestrator ticks re-aggregate the rewound range. Destructive: rows in that range are
    ///     temporarily out of sync until the orchestrator catches up.
    /// </summary>
    Task RewindRollupWatermarkAsync(string tenantId, string rollupRtId, DateTime toBucketEnd);

    /// <summary>
    ///     Returns every non-soft-deleted rollup archive attached to the given source CkArchive.
    /// </summary>
    Task<IReadOnlyList<RollupArchiveInfoDto>> ListRollupsForArchiveAsync(string tenantId, string archiveRtId);

    // ---- Archive data export/import (AB#4230) ----

    /// <summary>
    ///     Streams the raw CrateDB rows of an archive as <c>application/x-ndjson</c> (one row per
    ///     line). The returned <see cref="Stream"/> is the live HTTP response body — read it
    ///     line-by-line and never buffer the whole dataset. The caller owns the stream and must
    ///     dispose it. When both <paramref name="fromUtc"/> and <paramref name="toUtc"/> are
    ///     <c>null</c> the whole archive is exported; when supplied, only rows in the window
    ///     <c>[fromUtc, toUtc)</c> are emitted. Archive data export/import concept §4.2.
    /// </summary>
    /// <param name="tenantId">The tenant that owns the archive.</param>
    /// <param name="archiveRtId">Runtime id of the <c>CkArchive</c> entity.</param>
    /// <param name="fromUtc">Inclusive lower bound of the exported window (UTC); null for whole archive.</param>
    /// <param name="toUtc">Exclusive upper bound of the exported window (UTC); null for whole archive.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The live NDJSON response stream (not buffered).</returns>
    Task<Stream> ExportArchiveRowsAsync(string tenantId, string archiveRtId, DateTime? fromUtc, DateTime? toUtc,
        CancellationToken ct = default);

    /// <summary>
    ///     Streams archive rows into the target archive from an <c>application/x-ndjson</c> body.
    ///     The supplied <paramref name="ndjson"/> stream is streamed straight to the server (not
    ///     buffered). Archive data export/import concept §4.2.
    /// </summary>
    /// <param name="tenantId">The tenant that owns the archive.</param>
    /// <param name="archiveRtId">Runtime id of the <c>CkArchive</c> entity.</param>
    /// <param name="ndjson">The NDJSON row stream to import (one JSON object per line).</param>
    /// <param name="mode">Insert-only or upsert; serialized as a string query parameter.</param>
    /// <param name="ct">A cancellation token.</param>
    Task ImportArchiveRowsAsync(string tenantId, string archiveRtId, Stream ndjson, ArchiveImportMode mode,
        CancellationToken ct = default);

    /// <summary>
    ///     Returns the archive's schema (the <c>metadata.archive</c> block) for pre-flight
    ///     import schema-match validation. Archive data export/import concept §4.2 / §6.
    /// </summary>
    /// <param name="tenantId">The tenant that owns the archive.</param>
    /// <param name="archiveRtId">Runtime id of the <c>CkArchive</c> entity.</param>
    /// <param name="ct">A cancellation token.</param>
    Task<ArchiveSchemaDto> GetArchiveSchemaAsync(string tenantId, string archiveRtId,
        CancellationToken ct = default);
}