namespace Meshmakers.Octo.Sdk.ServiceClient.IdentityServices;

/// <summary>
///     AB#9044 (identity wave 2, Feature F5.2): messages of the <see cref="System.ObsoleteAttribute" /> on the REST methods
///     of <see cref="IIdentityServicesClient" /> whose endpoints became facades over the CK methods of
///     <c>System.Identity</c> 3.3.0 - 3.8.0. The replacement is the method client a consumer generates from
///     <c>System.Identity-[3.x,4.0)</c> (<c>construction-kits.yaml</c>); the lower bound of the range is the version in
///     which the REST endpoint became a facade over that method. See <c>docs/method-client-migration-guide.md</c>,
///     "Identity wave 2". The REST methods keep working during their REST window.
/// </summary>
internal static class IdentityMethodObsoleteMessages
{
    private const string Window =
        " (construction-kits.yaml); the REST method keeps working during its REST window. See docs/method-client-migration-guide.md (AB#9044).";

    private const string V33 = " generated from System.Identity-[3.3,4.0)" + Window;
    private const string V34 = " generated from System.Identity-[3.4,4.0)" + Window;
    private const string V35 = " generated from System.Identity-[3.5,4.0)" + Window;
    private const string V37 = " generated from System.Identity-[3.7,4.0)" + Window;
    private const string V38 = " generated from System.Identity-[3.8,4.0)" + Window;

    // Groups: membership and roles (3.3.0), create/update/delete (3.7.0)

    /// <summary>Replacement of <see cref="IIdentityServicesClient.CreateGroup" />.</summary>
    public const string CreateGroup = "Use RtGroupMethodClient.CreateGroupAsync" + V37;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.UpdateGroup" />.</summary>
    public const string UpdateGroup = "Use RtGroupMethodClient.UpdateGroupAsync" + V37;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.DeleteGroup" />.</summary>
    public const string DeleteGroup = "Use RtGroupMethodClient.DeleteGroupAsync" + V37;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.UpdateGroupRoles" />.</summary>
    public const string UpdateGroupRoles = "Use RtGroupMethodClient.SetRolesAsync" + V33;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.AddUserToGroup" />.</summary>
    public const string AddUserToGroup = "Use RtGroupMethodClient.AddMemberAsync (memberKind User)" + V33;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.RemoveUserFromGroup" />.</summary>
    public const string RemoveUserFromGroup = "Use RtGroupMethodClient.RemoveMemberAsync (memberKind User)" + V33;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.AddClientToGroup" />.</summary>
    public const string AddClientToGroup = "Use RtGroupMethodClient.AddMemberAsync (memberKind Client)" + V33;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.RemoveClientFromGroup" />.</summary>
    public const string RemoveClientFromGroup = "Use RtGroupMethodClient.RemoveMemberAsync (memberKind Client)" + V33;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.AddGroupToGroup" />.</summary>
    public const string AddGroupToGroup = "Use RtGroupMethodClient.AddChildGroupAsync" + V33;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.RemoveGroupFromGroup" />.</summary>
    public const string RemoveGroupFromGroup = "Use RtGroupMethodClient.RemoveChildGroupAsync" + V33;

    // Clients: roles and overlay (3.3.0), secrets (3.4.0), mirrors (3.5.0), create/update/delete (3.8.0)

    /// <summary>Replacement of <see cref="IIdentityServicesClient.CreateClient" />.</summary>
    public const string CreateClient = "Use RtClientMethodClient.CreateClientAsync" + V38;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.UpdateClient" />.</summary>
    public const string UpdateClient = "Use RtClientMethodClient.UpdateClientAsync" + V38;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.DeleteClient" />.</summary>
    public const string DeleteClient = "Use RtClientMethodClient.DeleteClientAsync" + V38;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.UpdateClientRoles" />.</summary>
    public const string UpdateClientRoles = "Use RtClientMethodClient.SetRolesAsync" + V33;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.AddRoleToClient" />.</summary>
    public const string AddRoleToClient = "Use RtClientMethodClient.AddRoleAsync" + V33;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.RemoveRoleFromClient" />.</summary>
    public const string RemoveRoleFromClient = "Use RtClientMethodClient.RemoveRoleAsync" + V33;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.ApplyClientOverlay" />.</summary>
    public const string ApplyClientOverlay = "Use RtClientMethodClient.ApplyOverlayUrisAsync" + V33;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.CleanOverlayEntries" />.</summary>
    public const string CleanOverlayEntries = "Use RtClientMethodClient.CleanOverlayEntriesAsync" + V33;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.CreateApiSecretForClient" />.</summary>
    public const string CreateApiSecretForClient = "Use RtClientMethodClient.AddSecretAsync" + V34;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.UpdateApiSecretClient" />.</summary>
    public const string UpdateApiSecretClient = "Use RtClientMethodClient.UpdateSecretAsync" + V34;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.DeleteApiSecretClient" />.</summary>
    public const string DeleteApiSecretClient = "Use RtClientMethodClient.RemoveSecretAsync" + V34;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.ProvisionClientInExistingTenants" />.</summary>
    public const string ProvisionClientInExistingTenants = "Use RtClientMethodClient.ProvisionMirrorsAsync" + V35;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.ProvisionClientInTenant" />.</summary>
    public const string ProvisionClientInTenant = "Use RtClientMethodClient.ProvisionMirrorInTenantAsync" + V35;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.UnprovisionClientFromTenant" />.</summary>
    public const string UnprovisionClientFromTenant = "Use RtClientMethodClient.RemoveMirrorAsync" + V35;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.SetClientAutoProvisionInChildTenants" />.</summary>
    public const string SetClientAutoProvisionInChildTenants = "Use RtClientMethodClient.SetMirrorAutoProvisionAsync" + V35;

    // Roles, API scopes, API resources (3.7.0; API resource secrets 3.4.0)

    /// <summary>Replacement of <see cref="IIdentityServicesClient.CreateRole" />.</summary>
    public const string CreateRole = "Use RtRoleMethodClient.CreateRoleAsync" + V37;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.UpdateRole" />.</summary>
    public const string UpdateRole = "Use RtRoleMethodClient.UpdateRoleAsync" + V37;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.DeleteRole" />.</summary>
    public const string DeleteRole = "Use RtRoleMethodClient.DeleteRoleAsync" + V37;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.CreateApiScope" />.</summary>
    public const string CreateApiScope = "Use RtApiScopeMethodClient.CreateApiScopeAsync" + V37;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.UpdateApiScope" />.</summary>
    public const string UpdateApiScope = "Use RtApiScopeMethodClient.UpdateApiScopeAsync" + V37;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.DeleteScope" />.</summary>
    public const string DeleteScope = "Use RtApiScopeMethodClient.DeleteApiScopeAsync" + V37;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.CreateApiResource" />.</summary>
    public const string CreateApiResource = "Use RtApiResourceMethodClient.CreateApiResourceAsync" + V37;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.UpdateApiResource" />.</summary>
    public const string UpdateApiResource = "Use RtApiResourceMethodClient.UpdateApiResourceAsync" + V37;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.DeleteApiResource" />.</summary>
    public const string DeleteApiResource = "Use RtApiResourceMethodClient.DeleteApiResourceAsync" + V37;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.CreateApiSecretForApiResource" />.</summary>
    public const string CreateApiSecretForApiResource = "Use RtApiResourceMethodClient.AddSecretAsync" + V34;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.UpdateApiSecretApiResource" />.</summary>
    public const string UpdateApiSecretApiResource = "Use RtApiResourceMethodClient.UpdateSecretAsync" + V34;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.DeleteApiSecretApiResource" />.</summary>
    public const string DeleteApiSecretApiResource = "Use RtApiResourceMethodClient.RemoveSecretAsync" + V34;

    // Data permissions and policies: grants and enforcement (3.4.0), create/delete (3.8.0)

    /// <summary>Replacement of <see cref="IIdentityServicesClient.GrantDataPermissionToRole" />.</summary>
    public const string GrantDataPermissionToRole = "Use RtDataPermissionMethodClient.GrantToRoleAsync" + V34;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.RevokeDataPermissionFromRole" />.</summary>
    public const string RevokeDataPermissionFromRole = "Use RtDataPermissionMethodClient.RevokeFromRoleAsync" + V34;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.SetDataPolicyEnforcementMode" />.</summary>
    public const string SetDataPolicyEnforcementMode = "Use RtDataPolicyMethodClient.SetEnforcementModeAsync" + V34;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.CreateDataPermission" />.</summary>
    public const string CreateDataPermission = "Use RtDataPermissionMethodClient.CreateDataPermissionAsync" + V38;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.DeleteDataPermission" />.</summary>
    public const string DeleteDataPermission = "Use RtDataPermissionMethodClient.DeleteDataPermissionAsync" + V38;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.CreateDataPolicy" />.</summary>
    public const string CreateDataPolicy = "Use RtDataPermissionMethodClient.CreatePolicyAsync" + V38;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.DeleteDataPolicy" />.</summary>
    public const string DeleteDataPolicy = "Use RtDataPolicyMethodClient.DeleteDataPolicyAsync" + V38;

    // External tenant user mappings and admin provisioning (3.5.0), mapping update/delete (3.8.0)

    /// <summary>Replacement of <see cref="IIdentityServicesClient.CreateExternalTenantUserMapping" />.</summary>
    public const string CreateExternalTenantUserMapping = "Use RtExternalTenantUserMappingMethodClient.CreateMappingAsync" + V35;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.GrantExternalTenantUser" />.</summary>
    public const string GrantExternalTenantUser = "Use RtExternalTenantUserMappingMethodClient.CreateMappingAsync" + V35;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.UpdateExternalTenantUserMapping" />.</summary>
    public const string UpdateExternalTenantUserMapping = "Use RtExternalTenantUserMappingMethodClient.UpdateMappingAsync" + V38;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.DeleteExternalTenantUserMapping" />.</summary>
    public const string DeleteExternalTenantUserMapping = "Use RtExternalTenantUserMappingMethodClient.DeleteMappingAsync" + V38;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.CreateAdminProvisioningMapping" />.</summary>
    public const string CreateAdminProvisioningMapping =
        "Use RtExternalTenantUserMappingMethodClient.ProvisionAdminAsync or ProvisionAdminWithGroupsAsync" + V35;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.ProvisionCurrentUser" />.</summary>
    public const string ProvisionCurrentUser = "Use RtExternalTenantUserMappingMethodClient.ProvisionCurrentUserAsync" + V35;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.DeleteAdminProvisioningMapping" />.</summary>
    public const string DeleteAdminProvisioningMapping =
        "Use RtExternalTenantUserMappingMethodClient.RemoveAdminProvisioningAsync" + V35;

    // Identity providers and e-mail domain group rules (3.8.0)

    /// <summary>Replacement of <see cref="IIdentityServicesClient.CreateIdentityProvider" />.</summary>
    public const string CreateIdentityProvider =
        "Use the method client of the concrete provider type, e.g. RtGoogleIdentityProviderMethodClient.CreateIdentityProviderAsync" + V38;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.UpdateIdentityProvider" />.</summary>
    public const string UpdateIdentityProvider =
        "Use the method client of the concrete provider type, e.g. RtGoogleIdentityProviderMethodClient.UpdateIdentityProviderAsync" + V38;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.DeleteIdentityProvider" />.</summary>
    public const string DeleteIdentityProvider =
        "Use the method client of the concrete provider type, e.g. RtGoogleIdentityProviderMethodClient.DeleteIdentityProviderAsync" + V38;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.CreateEmailDomainGroupRule" />.</summary>
    public const string CreateEmailDomainGroupRule = "Use RtEmailDomainGroupRuleMethodClient.CreateEmailDomainGroupRuleAsync" + V38;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.UpdateEmailDomainGroupRule" />.</summary>
    public const string UpdateEmailDomainGroupRule = "Use RtEmailDomainGroupRuleMethodClient.UpdateEmailDomainGroupRuleAsync" + V38;

    /// <summary>Replacement of <see cref="IIdentityServicesClient.DeleteEmailDomainGroupRule" />.</summary>
    public const string DeleteEmailDomainGroupRule = "Use RtEmailDomainGroupRuleMethodClient.DeleteEmailDomainGroupRuleAsync" + V38;
}
