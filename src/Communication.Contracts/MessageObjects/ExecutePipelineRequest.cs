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
}
