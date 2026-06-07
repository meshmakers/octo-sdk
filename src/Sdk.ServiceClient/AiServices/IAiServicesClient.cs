namespace Meshmakers.Octo.Sdk.ServiceClient.AiServices;

/// <summary>
///     Client proxy for the OctoMesh AI Adapter service. Phase 1 surface is just the
///     enable / disable lifecycle — session and quota operations live behind the SignalR hub
///     and the tenant-scoped REST API which are not yet covered by the SDK client.
/// </summary>
public interface IAiServicesClient : IServiceClient
{
    /// <summary>
    ///     Enables the AI Adapter for a tenant. The Communication Controller must already be
    ///     enabled on the same tenant — the AI service's CK model has a hard dependency on
    ///     System.Communication and the server returns HTTP 409 otherwise.
    /// </summary>
    /// <param name="tenantId">The id of the tenant.</param>
    Task EnableAsync(string tenantId);

    /// <summary>
    ///     Disables the AI Adapter for a tenant. The seeded AgentConfig / QuotaLimit and the
    ///     System.Ai CK model are not removed — re-enabling is idempotent.
    /// </summary>
    /// <param name="tenantId">The id of the tenant.</param>
    Task DisableAsync(string tenantId);
}
