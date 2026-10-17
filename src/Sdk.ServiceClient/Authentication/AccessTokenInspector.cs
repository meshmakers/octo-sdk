using System.Text;
using System.Text.Json;

namespace Meshmakers.Octo.Sdk.ServiceClient.Authentication;

/// <summary>
///     Reads claims of an access token without validating it (AB#6836). The service that receives the
///     token validates signature, issuer and lifetime; the client only needs to know which tenant a
///     token was issued for before it decides whether the token may be sent to a tenant's routes.
/// </summary>
public static class AccessTokenInspector
{
    /// <summary>
    ///     Name of the claim that carries the tenant a token was issued for.
    /// </summary>
    public const string TenantIdClaimType = "tenant_id";

    /// <summary>
    ///     Returns the <c>tenant_id</c> claim of a JWT access token, or <c>null</c> when the token is
    ///     blank, not a JWT or carries no such claim.
    /// </summary>
    /// <param name="accessToken">The access token.</param>
    public static string? TryGetTenantId(string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        var parts = accessToken.Split('.');
        if (parts.Length != 3)
        {
            return null;
        }

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            using var document = JsonDocument.Parse(Convert.FromBase64String(payload));
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty(TenantIdClaimType, out var claim) &&
                claim.ValueKind == JsonValueKind.String)
            {
                var value = claim.GetString();
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
        }
        catch (Exception ex) when (ex is FormatException or JsonException or ArgumentException)
        {
            // Not a readable JWT: the tenant cannot be determined.
        }

        return null;
    }

    /// <summary>
    ///     True when <paramref name="accessToken" /> is known to be issued for another tenant than
    ///     <paramref name="tenantId" />. A token whose tenant cannot be determined is not a mismatch.
    /// </summary>
    public static bool IsIssuedForOtherTenant(string? accessToken, string tenantId)
    {
        var tokenTenant = TryGetTenantId(accessToken);
        return tokenTenant != null && !string.Equals(tokenTenant, tenantId, StringComparison.OrdinalIgnoreCase);
    }

    internal static string Fingerprint(string value)
    {
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
