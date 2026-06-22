using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Request body for <c>POST {tenantId}/v1/clients/{id}/overlayUris</c> — declares a set
///     of overlay URIs to apply to the three URI list attributes of a blueprint-managed
///     <see cref="ClientDto"/>. The endpoint dedupes by URI string (any source) and writes
///     new entries with <c>Source = "overlay:&lt;OverlayName&gt;"</c>, so the Step 2a
///     preservation pass keeps them across blueprint re-apply and the future
///     <c>DumpTenant --clean</c> filter can strip them from sanitised exports.
/// </summary>
public sealed class ApplyOverlayUrisDto
{
    /// <summary>
    ///     The overlay name suffix that goes into the materialised
    ///     <c>Source = "overlay:&lt;OverlayName&gt;"</c> marker. Operator-meaningful (e.g.
    ///     <c>"local-dev"</c>, <c>"gerald-laptop"</c>). Constrained to <c>[A-Za-z0-9._-]+</c>
    ///     to match the identity-services <c>ClientUriSources.OverlayPrefix</c> contract and
    ///     the future <c>DumpTenant --clean</c> filter regex.
    /// </summary>
    [Required]
    [RegularExpression(@"^[A-Za-z0-9._-]+$",
        ErrorMessage = "OverlayName may only contain letters (A-Z, a-z), digits (0-9), and the characters '.', '-', and '_'.")]
    [Description("Operator-meaningful overlay name. Becomes the suffix of 'overlay:<OverlayName>' on every persisted entry.")]
    public required string OverlayName { get; init; }

    /// <summary>
    ///     URIs to add to <c>RedirectUris</c>. Null / empty list = no changes to that list.
    ///     Each URI is deduped against the existing list contents (any source); duplicates
    ///     are silently skipped.
    /// </summary>
    [Description("Redirect URIs to add as overlay entries. Existing duplicates are skipped silently.")]
    public List<string>? RedirectUris { get; init; }

    /// <summary>
    ///     URIs to add to <c>PostLogoutRedirectUris</c>. Same dedup rule as
    ///     <see cref="RedirectUris"/>.
    /// </summary>
    [Description("Post-logout redirect URIs to add as overlay entries. Existing duplicates are skipped silently.")]
    public List<string>? PostLogoutRedirectUris { get; init; }

    /// <summary>
    ///     CORS origins to add to <c>AllowedCorsOrigins</c>. Same dedup rule as
    ///     <see cref="RedirectUris"/>. The endpoint does NOT auto-strip trailing slashes —
    ///     the caller is responsible for passing origin-shaped values (no path / trailing
    ///     slash) so IdentityServer's <c>ValidatingClientStore</c> accepts them.
    /// </summary>
    [Description("CORS origins to add as overlay entries. Pass without trailing slash to satisfy IdentityServer's origin contract.")]
    public List<string>? AllowedCorsOrigins { get; init; }
}

/// <summary>
///     Response body for <c>POST {tenantId}/v1/clients/{id}/overlayUris</c> — per-list
///     breakdown of URIs the endpoint added vs. skipped as duplicates. Three independent
///     counts (RedirectUris / PostLogoutRedirectUris / AllowedCorsOrigins).
/// </summary>
public sealed class ApplyOverlayUrisResultDto
{
    /// <summary>The overlay name from the request — echoed back for client-side logging clarity.</summary>
    public required string OverlayName { get; init; }

    /// <summary>The client id the overlay was applied to (echoed for log correlation).</summary>
    public required string ClientId { get; init; }

    /// <summary>Counts for <c>RedirectUris</c>.</summary>
    public required ApplyOverlayUrisListCountDto RedirectUris { get; init; }

    /// <summary>Counts for <c>PostLogoutRedirectUris</c>.</summary>
    public required ApplyOverlayUrisListCountDto PostLogoutRedirectUris { get; init; }

    /// <summary>Counts for <c>AllowedCorsOrigins</c>.</summary>
    public required ApplyOverlayUrisListCountDto AllowedCorsOrigins { get; init; }
}

/// <summary>
///     Per-list result counts for a single overlay apply.
/// </summary>
public sealed class ApplyOverlayUrisListCountDto
{
    /// <summary>Number of new overlay entries appended to the list.</summary>
    public int Added { get; init; }

    /// <summary>Number of input URIs that already existed on the list (any source) and were skipped.</summary>
    public int SkippedDuplicate { get; init; }
}
