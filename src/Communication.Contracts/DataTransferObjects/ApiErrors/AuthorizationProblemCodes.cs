namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects.ApiErrors;

/// <summary>
///     Values of the <c>code</c> member of a refused authorization (<c>403</c>, body
///     <c>{"message": "...", "code": "..."}</c>). The codes are language-neutral and stable; clients branch on them, the
///     English message is for humans.
/// </summary>
public static class AuthorizationProblemCodes
{
    /// <summary>
    ///     AB#8529: the operation changes the tenant's data model (import a CK model, import from a catalog, FixAll,
    ///     blueprint install / update / uninstall, catalog refresh) and the caller lacks the scope <c>octo_api</c> or the
    ///     tenant role <c>DataModelManagement</c>. Grant the role (or membership in a group that carries it, e.g.
    ///     <c>TenantOwners</c>).
    /// </summary>
    public const string DataModelManagementRequired = "DataModelManagementRequired";
}
