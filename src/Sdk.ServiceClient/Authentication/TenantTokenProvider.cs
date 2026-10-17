using System.Collections.Concurrent;

namespace Meshmakers.Octo.Sdk.ServiceClient.Authentication;

/// <summary>
///     Default <see cref="ITenantTokenProvider" />. Same checks as the exchange of octo-mcp-service
///     (<c>TenantTokenExchanger</c>): both inputs present, a successful answer, an access token in
///     it. Added here: the issued token must carry the requested tenant, because a token for another
///     tenant would only fail later on the wire.
/// </summary>
public sealed class TenantTokenProvider : ITenantTokenProvider
{
    /// <summary>A cached token is renewed this long before it expires.</summary>
    public static readonly TimeSpan RenewalMargin = TimeSpan.FromSeconds(60);

    private readonly ITokenExchangeClient _exchangeClient;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.OrdinalIgnoreCase);
    private readonly ITenantTokenStore _store;
    private readonly TimeProvider _timeProvider;

    /// <summary>Creates the provider.</summary>
    public TenantTokenProvider(ITokenExchangeClient exchangeClient, ITenantTokenStore? store = null,
        TimeProvider? timeProvider = null)
    {
        _exchangeClient = exchangeClient;
        _store = store ?? new InMemoryTenantTokenStore();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task<string> GetAccessTokenAsync(string targetTenantId, string subjectToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetTenantId))
        {
            throw new ArgumentException("The target tenant is required.", nameof(targetTenantId));
        }

        if (string.IsNullOrWhiteSpace(subjectToken))
        {
            throw new TenantTokenExchangeDeniedException(targetTenantId, TenantTokenDenialCause.SubjectTokenRejected);
        }

        var fingerprint = AccessTokenInspector.Fingerprint(subjectToken);
        if (TryGetCached(targetTenantId, fingerprint, out var cached))
        {
            return cached;
        }

        var gate = _locks.GetOrAdd(targetTenantId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (TryGetCached(targetTenantId, fingerprint, out cached))
            {
                return cached;
            }

            var data = await _exchangeClient.ExchangeTokenForTenantAsync(subjectToken, targetTenantId,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(data.AccessToken) ||
                AccessTokenInspector.TryGetTenantId(data.AccessToken) is { } issuedFor &&
                !string.Equals(issuedFor, targetTenantId, StringComparison.OrdinalIgnoreCase))
            {
                throw new TenantTokenExchangeDeniedException(targetTenantId, TenantTokenDenialCause.InvalidResponse);
            }

            _store.Set(targetTenantId,
                new CachedTenantToken(data.AccessToken, data.ExpiresAt.ToUniversalTime(), fingerprint));
            return data.AccessToken;
        }
        finally
        {
            gate.Release();
        }
    }

    private bool TryGetCached(string tenantId, string fingerprint, out string accessToken)
    {
        var entry = _store.Get(tenantId);
        if (entry != null && entry.SubjectFingerprint == fingerprint &&
            entry.ExpiresAtUtc - RenewalMargin > _timeProvider.GetUtcNow().UtcDateTime)
        {
            accessToken = entry.AccessToken;
            return true;
        }

        accessToken = string.Empty;
        return false;
    }
}
