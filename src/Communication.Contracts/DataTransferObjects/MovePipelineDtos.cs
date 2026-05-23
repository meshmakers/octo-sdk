namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Body for <c>PATCH {tenantId}/v1/pipeline/move-to-adapter</c>. Reassigns
///     the <c>Pipeline.Executes</c> association of one or more pipelines from
///     their current adapter to <paramref name="TargetAdapterRtId"/>. Used when
///     a new adapter (e.g. one provisioned from a fresh Blueprint) replaces an
///     older adapter and the existing pipelines should keep running.
///
///     Server validates each source adapter's <c>CkTypeId</c> against the
///     target adapter's <c>CkTypeId</c> — only same-subtype moves are
///     accepted so a pipeline never lands on an adapter that cannot execute
///     its nodes. Each pipeline is moved atomically (old assoc removed +
///     new assoc inserted in one transaction); pipeline-level failures do
///     not abort the bulk run.
/// </summary>
public sealed record MovePipelinesToAdapterRequestDto(
    IReadOnlyList<string> PipelineRtIds,
    string TargetAdapterRtId,
    bool Redeploy);

/// <summary>
///     Outcome for a single pipeline inside a bulk move. <paramref name="Success"/>
///     is <c>true</c> iff the assoc swap committed cleanly; on failure
///     <paramref name="ErrorMessage"/> carries the reason. The old / new
///     adapter ids are filled in even on success so the caller can render
///     "moved from X to Y" toasts without an extra round-trip.
/// </summary>
public sealed record MovePipelineResultDto(
    string PipelineRtId,
    bool Success,
    string? OldAdapterRtId,
    string? NewAdapterRtId,
    string? ErrorMessage);

/// <summary>
///     Response of <see cref="MovePipelinesToAdapterRequestDto"/>. Always
///     returns 200 OK with the per-pipeline result list — even when every
///     pipeline failed — so the client can inspect each outcome without
///     parsing different HTTP-status shapes.
/// </summary>
public sealed record MovePipelinesToAdapterResponseDto(
    IReadOnlyList<MovePipelineResultDto> Results);
