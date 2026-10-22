using System.Security.Claims;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Record of a scratch tenant (AB#7192): a child tenant created with the opt-in flag <c>scratch</c> by a
///     client-credentials client of its direct parent. It names the creator, so identity can give exactly that
///     client's mirror the <c>TenantOwners</c> group in the child, and so the grant can be revoked for good.
/// </summary>
/// <remarks>
///     <para>
///         Persisted as JSON in the configuration of the <b>system tenant</b>, keyed by the child tenant id (see
///         <see cref="GetConfigurationKey" />). It deliberately does not live in the child's own database: it has to
///         exist before the child does (identity sets the child up the moment it is created), it must not be carried
///         along by a dump, and it must survive the restore that drops and re-creates the child.
///     </para>
///     <para>
///         Written by the asset repository (create); read, granted and revoked by
///         identity; removed with the tenant. A record is only honoured while <see cref="ParentTenantId" /> is the
///         tenant's actual parent.
///     </para>
/// </remarks>
public class ScratchTenantMarker
{
    /// <summary>Prefix of the system-tenant configuration key.</summary>
    public const string ConfigurationKeyPrefix = "ScratchTenant.";

    /// <summary>The system-tenant configuration key of the marker of <paramref name="tenantId" />.</summary>
    public static string GetConfigurationKey(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return ConfigurationKeyPrefix + tenantId.Trim().ToLowerInvariant();
    }

    /// <summary>
    ///     Decides whether <paramref name="user" /> may create a scratch tenant whose direct parent is
    ///     <paramref name="parentTenantId" /> and builds the marker. Returns the reason in words when it may not,
    ///     <c>null</c> when it may. The scratch grant is for agents, so the token must be a <b>client-credentials</b> token
    ///     (no user), issued for <b>the parent tenant itself</b>, and name its client. The role <c>TenantManagement</c> is
    ///     checked by the endpoint's own policy, not here.
    /// </summary>
    /// <param name="user">The caller's principal.</param>
    /// <param name="parentTenantId">The tenant that becomes (or is) the direct parent of the scratch tenant.</param>
    /// <param name="marker">The marker to record when the caller may; <c>null</c> otherwise.</param>
    public static string? TryCreateForCaller(ClaimsPrincipal user, string? parentTenantId,
        out ScratchTenantMarker? marker)
    {
        ArgumentNullException.ThrowIfNull(user);
        marker = null;

        // A client-credentials token has no user, i.e. no "sub" claim (checked in both the unmapped and the mapped form,
        // exactly like the tenant gate does).
        if (user.HasClaim(c => c.Type is "sub" or ClaimTypes.NameIdentifier))
        {
            return "A scratch tenant can only be created by a client-credentials client of the parent tenant, not by a " +
                   "user token. Create the tenant without the scratch flag and sign in to it.";
        }

        var clientId = user.FindFirst("client_id")?.Value;
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return "The token names no client (client_id claim), so the creator of the scratch tenant cannot be recorded.";
        }

        var tokenTenantId = user.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrWhiteSpace(parentTenantId) ||
            !string.Equals(tokenTenantId, parentTenantId, StringComparison.OrdinalIgnoreCase))
        {
            return "A scratch tenant can only be created with a token issued for its direct parent tenant " +
                   $"'{parentTenantId}'; this token was issued for " +
                   $"'{(string.IsNullOrWhiteSpace(tokenTenantId) ? "<none>" : tokenTenantId)}'.";
        }

        marker = new ScratchTenantMarker
        {
            ParentTenantId = parentTenantId.Trim().ToLowerInvariant(),
            CreatorClientId = clientId,
            CreatedAtUtc = DateTime.UtcNow
        };
        return null;
    }

    /// <summary>The direct parent tenant the creator client authenticated in when it created the tenant.</summary>
    public string ParentTenantId { get; set; } = string.Empty;

    /// <summary>Client id of the creator (the <c>client_id</c> claim of its token).</summary>
    public string CreatorClientId { get; set; } = string.Empty;

    /// <summary>When the tenant was created as a scratch tenant.</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>When identity first granted <c>TenantOwners</c> to the creator's mirror; null until then.</summary>
    public DateTime? GrantedAtUtc { get; set; }

    /// <summary>
    ///     When the grant was revoked; null while it is in force. A revoked marker is never granted again, however
    ///     often the tenant is set up or the mirror re-provisioned.
    /// </summary>
    public DateTime? RevokedAtUtc { get; set; }

    /// <summary>Who revoked the grant (user name or client id), for the audit trail.</summary>
    public string? RevokedBy { get; set; }
}
