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

    /// <summary>
    ///     Triggers (or coalesces) an optimistic recompute of a rollup archive over the half-open
    ///     range <c>[from, to)</c> (AB#4184), optionally scoped to a single <paramref name="rtIdScope"/>.
    ///     Returns the resulting job snapshot (state, counts, error reason). When a recompute is
    ///     already running for the archive, the range is folded into it and a <c>Coalesced</c> job is
    ///     returned.
    /// </summary>
    Task<RollupRecomputeJobInfoDto> RecomputeArchiveAsync(
        string tenantId, string rollupRtId, DateTime from, DateTime to, string? rtIdScope = null);

    /// <summary>
    ///     Returns the most recent recompute jobs for a rollup archive (newest first, capped at 50),
    ///     for operational debugging of why a recompute failed (AB#4184).
    /// </summary>
    Task<IReadOnlyList<RollupRecomputeJobInfoDto>> ListRecomputeJobsForArchiveAsync(
        string tenantId, string archiveRtId);

    /// <summary>
    ///     Adds a computed column to an Activated raw or time-range archive and backfills it across
    ///     the existing rows (AB#4189). The column stays hidden until the backfill completes, then
    ///     becomes visible atomically; a backfill failure leaves the previous archive state intact.
    ///     <paramref name="resultType"/> is the declared cast-back type name
    ///     (<c>Boolean</c> / <c>Int</c> / <c>Int64</c> / <c>Double</c> / <c>DateTime</c>).
    /// </summary>
    Task AddComputedColumnAsync(
        string tenantId, string archiveRtId, string name, string formula, string resultType, bool indexed = true);

    /// <summary>
    ///     Removes a computed column from an archive (AB#4189). Rejected when another computed column
    ///     still references it; the physical CrateDB column is left as a harmless orphan.
    /// </summary>
    Task RemoveComputedColumnAsync(string tenantId, string archiveRtId, string name);

    /// <summary>
    ///     Changes the formula of an existing computed column on an active archive with optimistic /
    ///     atomic semantics (AB#4189): readers keep the previous values while the new formula is
    ///     backfilled, then switch atomically. Rejected when another computed column references this
    ///     one. The result type is unchanged.
    /// </summary>
    Task UpdateComputedColumnFormulaAsync(string tenantId, string archiveRtId, string name, string formula);
}