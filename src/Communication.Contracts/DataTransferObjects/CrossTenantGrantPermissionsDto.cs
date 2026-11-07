namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     The allow-list of a tenant as a <b>source</b> of sibling-tenant grants (AB#8345): the target tenants whose
///     administrators may look up this tenant's users by exact user name or e-mail and grant them access.
///     <c>GET</c>/<c>PUT {tenantId}/v1/crossTenantGrantPermissions</c> of the identity service.
/// </summary>
/// <remarks>
///     An empty list means nobody, the default of every tenant. Only administrators of the tenant itself (roles
///     <c>UserManagement</c> and <c>TenantManagement</c>, token issued for that tenant) read or change it; anyone else gets
///     <c>403</c>. The service trims, lower-cases and de-duplicates the entries; each entry is at most 64 ASCII letters,
///     digits, <c>-</c> or <c>_</c>, at most 200 entries, never the tenant itself — otherwise <c>400</c> and nothing is
///     changed. Entries are not checked for existence. Contract: <c>docs/CONCEPT-SIBLING-TENANT-GRANTS.md</c> §6 in
///     octo-identity-services.
/// </remarks>
public record CrossTenantGrantPermissionsDto
{
    /// <summary>
    ///     The permitted target tenant ids. Required on <c>PUT</c>; <c>[]</c> withdraws every permission.
    /// </summary>
    public List<string> TargetTenantIds { get; init; } = [];
}
