namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Request of <c>POST ExternalTenantUserMappings/sourceUserLookup</c> in the target tenant (AB#8340): finds exactly one
///     user of a source tenant by exact user name or exact e-mail, so a target-tenant administrator can grant that user
///     access without seeing the source tenant's user directory.
/// </summary>
/// <remarks>
///     The identifier travels in the request body (POST) so that e-mail addresses do not end up in access logs. There is
///     no prefix, substring or wildcard semantics: the identity service compares the normalized identifier with the
///     normalized user name or e-mail only. Contract: <c>docs/CONCEPT-SIBLING-TENANT-GRANTS.md</c> in
///     octo-identity-services.
/// </remarks>
public record SourceUserLookupRequestDto
{
    /// <summary>
    ///     The tenant the user belongs to (registry ancestor or a sibling tenant that permits the target tenant).
    /// </summary>
    public string SourceTenantId { get; init; } = string.Empty;

    /// <summary>
    ///     Exact user name or exact e-mail address of the user in the source tenant (case-insensitive).
    /// </summary>
    public string Identifier { get; init; } = string.Empty;
}
