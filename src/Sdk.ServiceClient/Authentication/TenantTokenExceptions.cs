namespace Meshmakers.Octo.Sdk.ServiceClient.Authentication;

/// <summary>
///     Why a token for a tenant could not be obtained or used (AB#6836).
/// </summary>
public enum TenantTokenDenialCause
{
    /// <summary>The cause is not known; see the OAuth error.</summary>
    Unknown = 0,

    /// <summary>The token in use belongs to another tenant than the one addressed.</summary>
    TokenForOtherTenant,

    /// <summary>The identity service refused the exchange: the user may not access the tenant
    ///     (not an ancestor of it, or not provisioned there).</summary>
    AccessToTenantDenied,

    /// <summary>The user is known to be not provisioned in the tenant (explicit signal of the identity service).</summary>
    NotProvisioned,

    /// <summary>The client (octo-cli) is not allowed to use the token-exchange grant.</summary>
    ClientNotAllowed,

    /// <summary>The identity service did not accept the token to exchange (expired, revoked).</summary>
    SubjectTokenRejected,

    /// <summary>The target tenant is unknown to the identity service.</summary>
    TenantUnknown,

    /// <summary>The identity service could not be reached.</summary>
    IdentityUnreachable,

    /// <summary>The identity service answered without a usable token, or with a token for another tenant.</summary>
    InvalidResponse,

    /// <summary>The service accepted the token but refused the call (HTTP 403).</summary>
    Forbidden
}

/// <summary>
///     The client refused to send a token to the routes of a tenant it was not issued for. Raised
///     before any request leaves the process; the token is never sent as a fallback.
/// </summary>
[Serializable]
public class TenantTokenMismatchException : ServiceClientException
{
    /// <summary>Creates the exception.</summary>
    public TenantTokenMismatchException(string tenantId)
        : base(TenantTokenMessages.For(TenantTokenDenialCause.TokenForOtherTenant, tenantId, null))
    {
        TenantId = tenantId;
    }

    /// <summary>The tenant that was addressed.</summary>
    public string TenantId { get; }
}

/// <summary>
///     No token for the tenant could be obtained. The message names the tenant and the cause; the
///     root-tenant token is never used instead.
/// </summary>
[Serializable]
public class TenantTokenExchangeDeniedException : ServiceClientException
{
    /// <summary>Creates the exception.</summary>
    /// <param name="tenantId">The tenant a token was requested for.</param>
    /// <param name="cause">The cause.</param>
    /// <param name="oauthError">The OAuth error code of the identity service, if any (never a token).</param>
    /// <param name="inner">The inner exception.</param>
    public TenantTokenExchangeDeniedException(string tenantId, TenantTokenDenialCause cause,
        string? oauthError = null, Exception? inner = null)
        : base(TenantTokenMessages.For(cause, tenantId, oauthError), inner)
    {
        TenantId = tenantId;
        Cause = cause;
        OAuthError = oauthError;
    }

    /// <summary>The tenant a token was requested for.</summary>
    public string TenantId { get; }

    /// <summary>The cause of the denial.</summary>
    public TenantTokenDenialCause Cause { get; }

    /// <summary>The OAuth error code, if the identity service sent one.</summary>
    public string? OAuthError { get; }
}

/// <summary>
///     Messages per denial cause. They name the tenant and the required step, never a token.
/// </summary>
public static class TenantTokenMessages
{
    /// <summary>Builds the message for a cause.</summary>
    public static string For(TenantTokenDenialCause cause, string tenantId, string? oauthError)
    {
        var provision = $"An administrator of tenant '{tenantId}' must provision your user there " +
                        $"(octo-cli -c ProvisionCurrentUser -ttid {tenantId}), also for newly created tenants.";
        var detail = string.IsNullOrWhiteSpace(oauthError) ? string.Empty : $" (identity error: {oauthError})";

        return cause switch
        {
            TenantTokenDenialCause.TokenForOtherTenant =>
                $"The access token in use was issued for another tenant than '{tenantId}' and is not sent to it. " +
                $"Run the command with a context for tenant '{tenantId}' or use a client that can exchange the token.",
            TenantTokenDenialCause.NotProvisioned =>
                $"Your user is not provisioned in tenant '{tenantId}'. {provision}",
            TenantTokenDenialCause.AccessToTenantDenied =>
                $"No token for tenant '{tenantId}' was issued: your user may not access that tenant{detail}. " +
                $"The tenant must be a descendant of your tenant and your user must be provisioned in it. {provision}",
            TenantTokenDenialCause.ClientNotAllowed =>
                $"No token for tenant '{tenantId}' was issued: this client is not allowed to exchange tokens{detail}. " +
                "The identity service must allow the token-exchange grant for the client; use a context for the tenant instead.",
            TenantTokenDenialCause.SubjectTokenRejected =>
                $"No token for tenant '{tenantId}' was issued: the identity service did not accept your current " +
                $"sign-in{detail}. Sign in again (octo-cli -c LogIn).",
            TenantTokenDenialCause.TenantUnknown =>
                $"No token for tenant '{tenantId}' was issued: the tenant is not known to the identity service{detail}.",
            TenantTokenDenialCause.IdentityUnreachable =>
                $"No token for tenant '{tenantId}' was issued: the identity service could not be reached.",
            TenantTokenDenialCause.InvalidResponse =>
                $"No token for tenant '{tenantId}' was issued: the identity service answered without a usable token " +
                "for that tenant.",
            TenantTokenDenialCause.Forbidden =>
                $"Access to tenant '{tenantId}' was denied (HTTP 403) although a token for that tenant was used. " +
                $"Your user has no role that permits this call there. {provision}",
            _ =>
                $"No token for tenant '{tenantId}' was issued{detail}."
        };
    }
}
