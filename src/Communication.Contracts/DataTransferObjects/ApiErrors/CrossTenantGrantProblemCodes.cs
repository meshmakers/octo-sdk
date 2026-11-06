namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects.ApiErrors;

/// <summary>
///     Values of the <c>code</c> extension of the RFC 9457 problem answers of the cross-tenant grant endpoints of the
///     identity service (AB#8340). The codes are language-neutral and stable; clients branch on them, the English title
///     and detail are for humans. Contract: <c>docs/CONCEPT-SIBLING-TENANT-GRANTS.md</c> in octo-identity-services.
/// </summary>
public static class CrossTenantGrantProblemCodes
{
    /// <summary>
    ///     <c>404</c> of <c>POST ExternalTenantUserMappings/sourceUserLookup</c>. One answer for every negative case
    ///     (user or tenant absent, source tenant does not permit the target, shadow user, locked or blocked user, more
    ///     than one match), so the answer reveals nothing about the source tenant.
    /// </summary>
    public const string SourceUserNotFound = "SourceUserNotFound";

    /// <summary>
    ///     <c>429</c> of <c>POST ExternalTenantUserMappings/sourceUserLookup</c> when the caller exceeded the lookup limit
    ///     for the target tenant; the answer carries <c>Retry-After</c>.
    /// </summary>
    public const string SourceUserLookupRateLimited = "SourceUserLookupRateLimited";

    /// <summary>
    ///     <c>403</c> of <c>POST ExternalTenantUserMappings</c> when the source tenant is not a registry ancestor of the
    ///     target tenant and does not permit it, or the source user does not exist or is blocked there. One answer for
    ///     all these cases.
    /// </summary>
    public const string SourceTenantNotPermitted = "SourceTenantNotPermitted";

    /// <summary>
    ///     <c>400</c> of <c>POST ExternalTenantUserMappings</c> when a <c>groupIds</c> entry is no group of the target
    ///     tenant. Nothing is stored; the answer lists the unknown ids in the <c>unknownGroupIds</c> extension.
    /// </summary>
    public const string UnknownGroup = "UnknownGroup";
}
