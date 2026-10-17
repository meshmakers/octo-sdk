namespace Meshmakers.Octo.Sdk.ServiceClient.Authentication;

/// <summary>
///     Exchanges the token a client is signed in with for a token issued for another tenant
///     (RFC 8693) and caches it per tenant (AB#6836).
/// </summary>
public interface ITenantTokenProvider
{
    /// <summary>
    ///     Returns an access token issued for <paramref name="targetTenantId" />. Re-exchanges silently from
    ///     <paramref name="subjectToken" /> when the cached token is missing, about to expire or was
    ///     obtained from another subject token.
    /// </summary>
    /// <param name="targetTenantId">The tenant to address.</param>
    /// <param name="subjectToken">The token the caller is signed in with.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="TenantTokenExchangeDeniedException">No token could be obtained. Never falls back to the subject token.</exception>
    Task<string> GetAccessTokenAsync(string targetTenantId, string subjectToken,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     Performs the RFC 8693 exchange against the identity service.
/// </summary>
public interface ITokenExchangeClient
{
    /// <summary>
    ///     Exchanges <paramref name="subjectToken" /> for an access token issued for
    ///     <paramref name="targetTenantId" />. Exchanged tokens carry no refresh token.
    /// </summary>
    /// <exception cref="TenantTokenExchangeDeniedException">The identity service refused or could not be reached.</exception>
    Task<AuthenticationData> ExchangeTokenForTenantAsync(string subjectToken, string targetTenantId,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     A token issued for a tenant, as kept by an <see cref="ITenantTokenStore" />.
/// </summary>
/// <param name="AccessToken">The access token.</param>
/// <param name="ExpiresAtUtc">When it expires.</param>
/// <param name="SubjectFingerprint">Fingerprint of the token it was exchanged from.</param>
public sealed record CachedTenantToken(string AccessToken, DateTime ExpiresAtUtc, string SubjectFingerprint);

/// <summary>
///     Keeps exchanged tokens per tenant. The default keeps them in memory; a host may persist them
///     where it already protects the token they were exchanged from.
/// </summary>
public interface ITenantTokenStore
{
    /// <summary>Returns the cached token for a tenant, if any.</summary>
    CachedTenantToken? Get(string tenantId);

    /// <summary>Stores the token for a tenant.</summary>
    void Set(string tenantId, CachedTenantToken token);
}

/// <summary>
///     In-memory <see cref="ITenantTokenStore" />.
/// </summary>
public sealed class InMemoryTenantTokenStore : ITenantTokenStore
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, CachedTenantToken> _tokens =
        new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public CachedTenantToken? Get(string tenantId) => _tokens.GetValueOrDefault(tenantId);

    /// <inheritdoc />
    public void Set(string tenantId, CachedTenantToken token) => _tokens[tenantId] = token;
}
