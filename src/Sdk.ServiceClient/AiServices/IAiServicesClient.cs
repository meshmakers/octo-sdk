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

    /// <summary>
    ///     Redeems a one-time credential ticket and persists the freshly minted Anthropic
    ///     subscription tokens on the server. Anonymous: the ticket code is the auth
    ///     artefact, so the bastion CLI does NOT need an OctoMesh user session — the
    ///     <see cref="AiServicesClient" /> implementation bypasses the bearer header for
    ///     this call. Idempotency: a ticket can be redeemed exactly once; subsequent
    ///     attempts surface as a non-success response with HTTP 400 / 410.
    /// </summary>
    /// <param name="tenantId">Tenant the ticket was issued for.</param>
    /// <param name="code">One-time code shown by the admin's Refinery Studio modal.</param>
    /// <param name="accessToken">Plaintext Anthropic access token.</param>
    /// <param name="refreshToken">Plaintext Anthropic refresh token.</param>
    /// <param name="accessExpiresAt">UTC expiry of the access token.</param>
    /// <param name="refreshExpiresAt">UTC expiry of the refresh token.</param>
    Task<CredentialsStatusDto> RedeemTicketAsync(
        string tenantId,
        string code,
        string accessToken,
        string refreshToken,
        DateTime accessExpiresAt,
        DateTime refreshExpiresAt);

    /// <summary>
    ///     Returns the current lease snapshot for the route tenant. Returns a DTO with
    ///     <c>Status="NoLease"</c> when the tenant has never registered a subscription —
    ///     that's the initial state, not an error. Requires an authenticated tenant-scoped
    ///     client (<see cref="AiServiceClientOptions.TenantId" /> must be set).
    /// </summary>
    Task<CredentialsStatusDto> GetCredentialsStatusAsync();

    /// <summary>
    ///     Marks the route tenant's lease as <c>Revoked</c>. The ciphertext is kept for
    ///     audit so an operator can still inspect what was registered, but new sessions
    ///     refuse to start. Requires an authenticated tenant-scoped client.
    /// </summary>
    Task<CredentialsStatusDto> RevokeCredentialsAsync();

    /// <summary>
    ///     Reads key source, monthly LLM cap and spend (current and previous UTC month) of the client's own tenant
    ///     (<c>{tenant}/v1/aiSpend</c>). Read-only; a token of another tenant is refused.
    /// </summary>
    Task<AiSpendReportDto> GetAiSpendAsync();

    /// <summary>
    ///     Lists the caps of all tenants that have one (<c>system/v1/aiSpendCaps</c>). Needs a system-tenant token with the
    ///     role <c>AiBudgetAdmin</c> and full API access.
    /// </summary>
    Task<IReadOnlyList<AiSpendReportDto>> ListAiSpendCapsAsync();

    /// <summary>Reads cap and spend of a tenant as platform admin (role <c>AiBudgetAdmin</c>, system tenant).</summary>
    /// <param name="tenantId">Tenant whose cap is read.</param>
    Task<AiSpendReportDto> GetAiSpendCapAsync(string tenantId);

    /// <summary>
    ///     Sets key source and monthly cap of a tenant (role <c>AiBudgetAdmin</c>, system tenant). Every change writes an
    ///     audit record with old and new value, who and why; it takes effect in the gateway within at most 60 seconds.
    /// </summary>
    /// <param name="tenantId">Tenant whose cap is set; must exist.</param>
    /// <param name="request">New cap, key source and reason.</param>
    Task<AiSpendCapChangeDto> SetAiSpendCapAsync(string tenantId, SetAiSpendCapRequestDto request);

    /// <summary>Reads the newest audit records of a tenant's cap changes (role <c>AiBudgetAdmin</c>, system tenant).</summary>
    /// <param name="tenantId">Tenant.</param>
    /// <param name="limit">Most records (1..500).</param>
    Task<IReadOnlyList<AiSpendCapAuditDto>> GetAiSpendCapAuditAsync(string tenantId, int limit = 50);
}
