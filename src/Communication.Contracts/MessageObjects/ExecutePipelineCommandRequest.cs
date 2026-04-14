namespace Meshmakers.Octo.Communication.Contracts.MessageObjects;

/// <summary>
/// Request for executing a pipeline via the distribution event hub
/// </summary>
public record ExecutePipelineCommandRequest
{
    /// <summary>
    /// Creates a new request
    /// </summary>
    /// <param name="tenantId">Tenant id</param>
    /// <param name="pipelineInput">Optional pipeline input as JSON</param>
    public ExecutePipelineCommandRequest(string tenantId, string? pipelineInput)
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
}
