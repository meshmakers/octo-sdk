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
    ///     Disables StreamData for a tenant: switches the tenant flag off. The <c>System.StreamData</c> model,
    ///     the archive definitions and the stored stream data are kept; <see cref="EnableAsync" /> restores access.
    /// </summary>
    /// <remarks>
    ///     Verified precondition, not a teardown (AB#4255): the service answers HTTP 409 with an
    ///     <c>OperationFailedErrorDto</c> naming every archive that is still <c>Activated</c> - surfaced here as a
    ///     <c>ServiceClientResultException</c> - until they are disabled (<see cref="DisableArchiveAsync" />, data
    ///     kept) or deleted (<see cref="DeleteArchiveAsync" />, rollups before their source). Disabling StreamData
    ///     is required before the tenant can be deleted or detached.
    /// </remarks>
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
    ///     Returns the measured data coverage of the given archive and of every archive related to
    ///     it in the tenant's stream-data landscape (AB#5157): what each archive stores, and the
    ///     timestamp range it actually holds data for.
    /// </summary>
    /// <param name="tenantId">The tenant that owns the archive.</param>
    /// <param name="archiveRtId">Runtime id of the <c>CkArchive</c> entity to report coverage for.</param>
    Task<IReadOnlyList<ArchiveCoverageDto>> GetArchiveCoverageAsync(string tenantId, string archiveRtId);

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
    ///     Populates / resets a rollup over the ENTIRE history of its source archive without supplying
    ///     a timestamp (AB#4269). Resolves the source archive's earliest timestamp and recomputes
    ///     <c>[sourceMin, now)</c> over the same reader-safe optimistic recompute path as
    ///     <see cref="RecomputeArchiveAsync"/>. Returns the resulting job snapshot, or <c>null</c> when
    ///     the source archive holds no data (no-op).
    /// </summary>
    Task<RollupRecomputeJobInfoDto?> BackfillRollupFromSourceAsync(string tenantId, string rollupRtId);

    /// <summary>
    ///     Returns the most recent recompute jobs for a rollup archive (newest first, capped at 50),
    ///     for operational debugging of why a recompute failed (AB#4184).
    /// </summary>
    Task<IReadOnlyList<RollupRecomputeJobInfoDto>> ListRecomputeJobsForArchiveAsync(
        string tenantId, string archiveRtId);

    /// <summary>
    ///     Checks rollups for the inconsistencies a restore (or a dump taken during a recompute) can leave: a pointer
    ///     without rows, rows above the pointer, a watermark ahead of the rows, a missing generation map (AB#6968).
    ///     Read only; the server runs counts and MAX/MIN statements, nothing is changed. An inconsistent rollup is
    ///     reported, not thrown.
    /// </summary>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="rollupRtIds">The rollups to check; <c>null</c> or empty checks every rollup archive of the tenant.</param>
    Task<RollupConsistencyReportDto> ValidateRollupsAsync(string tenantId,
        IReadOnlyCollection<string>? rollupRtIds = null);

    /// <summary>
    ///     Lists the steps a rollup repair would run per rollup and changes nothing (dry run, AB#6968). The repair
    ///     itself is <see cref="EnqueueRollupRepairAsync" />.
    /// </summary>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="rollupRtIds">The rollups to plan for; <c>null</c> or empty plans every rollup archive of the tenant.</param>
    Task<RollupRepairReportDto> PlanRollupRepairAsync(string tenantId, IReadOnlyCollection<string>? rollupRtIds = null);

    /// <summary>
    ///     Requests the repair of what <see cref="ValidateRollupsAsync" /> finds (AB#6968). Idempotent. The server runs
    ///     nothing inside this call: it answers 202 with a request (<see cref="RollupRepairRequestDto.RequestId" />) and
    ///     executes the repair in the background; a second request while one is active returns the active one. Follow
    ///     it with <see cref="GetRollupRepairRequestAsync" />.
    /// </summary>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="rollupRtIds">The rollups to repair; <c>null</c> or empty repairs every rollup archive of the tenant.</param>
    Task<RollupRepairRequestDto> EnqueueRollupRepairAsync(string tenantId,
        IReadOnlyCollection<string>? rollupRtIds = null);

    /// <summary>
    ///     Reads the state of a repair request (AB#6968): <c>Pending</c>, <c>Running</c>, <c>Done</c> or <c>Failed</c>,
    ///     with the final report once it has ended.
    /// </summary>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="requestId">The id returned by <see cref="EnqueueRollupRepairAsync" />.</param>
    Task<RollupRepairRequestDto> GetRollupRepairRequestAsync(string tenantId, string requestId);

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

    /// <summary>
    ///     Writes externally aggregated time-range values into an Activated <c>TimeRangeArchive</c>
    ///     (<c>POST streamdata/archives/{archiveRtId}/insertTimeRange</c>). Each point declares its
    ///     <see cref="TimeRangePointDto.Origin" /> (default automated) and, for a manual write, an optional
    ///     reason; the service records the writer of a manual value from the caller's token (AB#7593,
    ///     AB#7596). A manual write needs the tenant's read-write permission like every write.
    ///     Returns how many rows were stored and which were not, with the reason (AB#7598); a manual value
    ///     that could not be stored is a refused call (HTTP 409), never a result.
    /// </summary>
    Task<StreamDataWriteResultDto> InsertTimeRangeAsync(string tenantId, string archiveRtId, IReadOnlyCollection<TimeRangePointDto> points);
}
