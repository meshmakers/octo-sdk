namespace Meshmakers.Octo.Communication.Contracts.MessageObjects;

/// <summary>
/// Arguments for executing a mesh pipeline via the distribution event hub
/// </summary>
public record ExecutePipelineRequest
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="tenantId">Tenant id</param>
    /// <param name="pipelineInput">Optional pipeline input</param>
    public ExecutePipelineRequest(string tenantId, string? pipelineInput)
    {
        TenantId = tenantId;
        PipelineInput = pipelineInput;
    }

    /// <summary>
    /// Returns the tenant id
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// An optional value as pipeline input
    /// </summary>
    public string? PipelineInput { get; init; }

    /// <summary>
    /// When true, the adapter executes the pipeline with all Load-node side
    /// effects suppressed (M4-B.2 dry-run). Load nodes that honour the flag
    /// record their would-be payload via the debug stream instead of firing
    /// their real sink. Default false preserves classic real-effect semantics.
    /// </summary>
    public bool IsDryRun { get; init; }

    /// <summary>
    /// The invoker of this manual execution, carried through so the pipeline can run as them
    /// (AB#5126). Null when the request was published without a caller (an internal invocation, or
    /// an older controller) — the pipeline then runs anonymously exactly as before. Token-free by
    /// design; see <see cref="CallerAccessToken" /> for the credential.
    /// </summary>
    public ExecutePipelineCaller? Caller { get; init; }

    /// <summary>
    /// The <b>raw access token</b> the invoker presented, for a node that must act as the invoker
    /// against another service (delegation / "on-behalf-of", AB#5031/5026). Null when none was
    /// carried. Deliberately separate from <see cref="Caller" /> so the credential never reaches the
    /// pipeline data root; never log it.
    /// </summary>
    public string? CallerAccessToken { get; init; }
}
