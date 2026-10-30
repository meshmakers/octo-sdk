// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Result of revoking all sessions and tokens of one user in a tenant
///     (<c>POST {tenant}/v1/users/{userName}/revokeSessions</c>, AB#7899).
/// </summary>
public record UserSessionRevocationResultDto
{
    /// <summary>The user name.</summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>The subject (user id).</summary>
    public string Subject { get; init; } = string.Empty;

    /// <summary>The security stamp was rotated.</summary>
    public bool SecurityStampRotated { get; init; }

    /// <summary>OAuth tokens (refresh tokens, codes) revoked.</summary>
    public long RevokedTokens { get; init; }

    /// <summary>OAuth authorizations revoked.</summary>
    public long RevokedAuthorizations { get; init; }

    /// <summary>Server-side browser sessions removed.</summary>
    public long RemovedSessions { get; init; }
}

/// <summary>
///     Result of the bulk revocation for the cross-tenant shadow users (<c>xt_*</c>) of a tenant
///     (<c>POST {tenant}/v1/users/revokeSessions?scope=shadowUsers</c>, AB#7899).
/// </summary>
public record ShadowUserSessionRevocationResultDto
{
    /// <summary><c>true</c>: nothing was changed, the counts show what an apply would revoke.</summary>
    public bool DryRun { get; init; }

    /// <summary>The selection (<c>shadowUsers</c>).</summary>
    public string Scope { get; init; } = string.Empty;

    /// <summary>The tenant.</summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>Shadow users selected.</summary>
    public int Users { get; init; }

    /// <summary>Valid OAuth tokens of those users.</summary>
    public long Tokens { get; init; }

    /// <summary>Valid OAuth authorizations of those users.</summary>
    public long Authorizations { get; init; }

    /// <summary>Server-side browser sessions of those users.</summary>
    public long Sessions { get; init; }

    /// <summary>The first (up to 100) user names.</summary>
    public IReadOnlyList<string> UserNames { get; init; } = [];
}
