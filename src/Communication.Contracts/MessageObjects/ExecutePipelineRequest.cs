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
}
