namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Response body for <c>DELETE {tenantId}/v1/clients/cleanOverlayEntries</c> — strips
///     entries where <c>Source</c> starts with <c>overlay:</c> (or matches a specific
///     <c>overlay:&lt;name&gt;</c> if a name was supplied) from every blueprint-managed
///     client's URI lists. Companion to the <see cref="ApplyOverlayUrisDto"/> family
///     introduced in AB#4209 Step 4 — together they let operators apply and rescind
///     overlay URIs declaratively without modifying the blueprint seed.
/// </summary>
public sealed class CleanOverlayEntriesResultDto
{
    /// <summary>
    ///     The overlay name filter that was applied, or <c>null</c> if all
    ///     <c>overlay:*</c> sources were targeted.
    /// </summary>
    public string? OverlayName { get; init; }

    /// <summary>
    ///     Number of clients that had at least one matching entry removed (and thus
    ///     received an <c>UpdateAsync</c> + cache invalidation).
    /// </summary>
    public int ClientsAffected { get; init; }

    /// <summary>
    ///     Total number of URI entries removed across every list across every client.
    /// </summary>
    public int TotalEntriesRemoved { get; init; }

    /// <summary>
    ///     Per-client breakdown — one entry per client that had at least one matching
    ///     URI removed (clients with zero removals are omitted to keep the response small).
    /// </summary>
    public required List<CleanOverlayEntriesClientResultDto> ClientResults { get; init; }
}

/// <summary>
///     Per-client breakdown of removed entries.
/// </summary>
public sealed class CleanOverlayEntriesClientResultDto
{
    /// <summary>The ClientId the counts apply to.</summary>
    public required string ClientId { get; init; }

    /// <summary>Number of entries removed from <c>RedirectUris</c>.</summary>
    public int RedirectUrisRemoved { get; init; }

    /// <summary>Number of entries removed from <c>PostLogoutRedirectUris</c>.</summary>
    public int PostLogoutRedirectUrisRemoved { get; init; }

    /// <summary>Number of entries removed from <c>AllowedCorsOrigins</c>.</summary>
    public int AllowedCorsOriginsRemoved { get; init; }
}
