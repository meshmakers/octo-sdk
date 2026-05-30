namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Body for <c>PATCH {tenantId}/v1/pipeline/{pipelineRtId}/debug</c>.
/// </summary>
/// <param name="Enabled">true to enable debug capture for the pipeline, false to disable.</param>
public sealed record SetPipelineDebugRequestDto(bool Enabled);

/// <summary>
///     Result of toggling pipeline debug mode.
/// </summary>
/// <param name="Enabled">The persisted debug state after the change.</param>
/// <param name="AppliedToRunningAdapter">
///     true when the change was pushed to a live adapter and is effective immediately;
///     false when the owning adapter was offline, so the change is persisted only and will
///     take effect on the next deploy.
/// </param>
public sealed record SetPipelineDebugResultDto(bool Enabled, bool AppliedToRunningAdapter);

/// <summary>
///     Current persisted debug state of a pipeline
///     (response of <c>GET {tenantId}/v1/pipeline/{pipelineRtId}/debug</c>).
/// </summary>
/// <param name="Enabled">true if debug capture is currently enabled for the pipeline.</param>
public sealed record PipelineDebugStateDto(bool Enabled);
