// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Data Transfer Object of a Octo client
/// </summary>
public class ClientDto
{
    /// <summary>
    ///     Specifies if client is enabled
    /// </summary>
    public bool? IsEnabled { get; set; }

    /// <summary>
    ///     Unique ID of the client
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    ///     Optional client secret
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    ///     Specifies if a client secret is required to request tokens
    /// </summary>
    public bool? RequireClientSecret { get; set; }

    /// <summary>
    ///     Client display name (used for logging and consent screen)
    /// </summary>
    public string? ClientName { get; set; }

    /// <summary>
    ///     URI to further information about client (used on consent screen)
    /// </summary>
    public string? ClientUri { get; set; }

    /// <summary>
    ///     Specifies the allowed grant types (legal combinations of AuthorizationCode, Implicit, Hybrid, ResourceOwner,
    ///     ClientCredentials).
    /// </summary>
    public IEnumerable<string>? AllowedGrantTypes { get; set; }

    /// <summary>
    ///     Specifies allowed URIs to return tokens or authorization codes to
    /// </summary>
    public IEnumerable<string>? RedirectUris { get; set; }

    /// <summary>
    ///     Specifies allowed URIs to redirect to after logout
    /// </summary>
    public IEnumerable<string>? PostLogoutRedirectUris { get; set; }

    /// <summary>
    ///     Gets or sets the allowed CORS origins for JavaScript clients.
    /// </summary>
    public IEnumerable<string>? AllowedCorsOrigins { get; set; }

    /// <summary>
    ///     Specifies the api scopes that the client is allowed to request
    /// </summary>
    public IEnumerable<string>? AllowedScopes { get; set; }

    /// <summary>
    ///     Specifies if offline access to use code_authorization is enabled
    /// </summary>
    public bool? IsOfflineAccessEnabled { get; set; }

    /// <summary>
    ///     Specifies the front-channel logout URI for Single Logout (SLO).
    ///     The Identity Server will load this URI in an iframe during logout.
    /// </summary>
    public string? FrontChannelLogoutUri { get; set; }

    /// <summary>
    ///     Specifies whether session ID is required for front-channel logout.
    /// </summary>
    public bool? FrontChannelLogoutSessionRequired { get; set; }

    /// <summary>
    ///     Specifies the back-channel logout URI for Single Logout (SLO).
    ///     The Identity Server will POST a logout token to this URI.
    /// </summary>
    public string? BackChannelLogoutUri { get; set; }

    /// <summary>
    ///     Specifies whether session ID is required for back-channel logout.
    /// </summary>
    public bool? BackChannelLogoutSessionRequired { get; set; }

    /// <summary>
    ///     When true and this is a ClientCredentials client living in a parent tenant,
    ///     every new child tenant gets a mirror of this client auto-provisioned.
    ///     Enables a single ClientCredentials identity (typically a CI/CD agent) to
    ///     reach every tenant on the instance without per-tenant manual setup.
    ///     Defaults to <c>false</c>.
    /// </summary>
    public bool? AutoProvisionInChildTenants { get; set; }

    /// <summary>
    ///     When set, this client is a mirror provisioned from the named parent tenant.
    ///     Sub-tenant UIs surface this as a read-only "Provisioned by parent tenant"
    ///     indicator; the client must not be edited locally because the next sync
    ///     would overwrite the change. <c>null</c> on locally-owned clients.
    /// </summary>
    public string? ProvisionedByParentTenantId { get; set; }
}