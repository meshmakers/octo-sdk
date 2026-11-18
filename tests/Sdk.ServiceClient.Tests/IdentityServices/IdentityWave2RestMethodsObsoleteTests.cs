using System.Reflection;
using Meshmakers.Octo.Sdk.ServiceClient.IdentityServices;

namespace Sdk.ServiceClient.Tests.IdentityServices;

/// <summary>
///     AB#9044 (identity wave 2): the REST methods of <see cref="IIdentityServicesClient" /> whose endpoints became
///     facades over the CK methods of <c>System.Identity</c> 3.3.0 - 3.8.0 are obsolete on the interface and on the
///     client. The message names the replacement in the method client a consumer generates and the lowest
///     <c>System.Identity</c> version whose REST endpoint is a facade over that method. Warnings, not errors: the REST
///     methods keep calling REST during their REST window. Reads (Get*, LookupSourceUser, the cross-tenant grant
///     permissions) and the user methods outside the waves stay as they are.
/// </summary>
public class IdentityWave2RestMethodsObsoleteTests
{
    /// <summary>REST method, replacement (client and method), lowest System.Identity minor of the range.</summary>
    public static TheoryData<string, string, string> Wave2() => new()
    {
        // Groups
        { "CreateGroup", "RtGroupMethodClient.CreateGroupAsync", "3.7" },
        { "UpdateGroup", "RtGroupMethodClient.UpdateGroupAsync", "3.7" },
        { "DeleteGroup", "RtGroupMethodClient.DeleteGroupAsync", "3.7" },
        { "UpdateGroupRoles", "RtGroupMethodClient.SetRolesAsync", "3.3" },
        { "AddUserToGroup", "RtGroupMethodClient.AddMemberAsync (memberKind User)", "3.3" },
        { "RemoveUserFromGroup", "RtGroupMethodClient.RemoveMemberAsync (memberKind User)", "3.3" },
        { "AddClientToGroup", "RtGroupMethodClient.AddMemberAsync (memberKind Client)", "3.3" },
        { "RemoveClientFromGroup", "RtGroupMethodClient.RemoveMemberAsync (memberKind Client)", "3.3" },
        { "AddGroupToGroup", "RtGroupMethodClient.AddChildGroupAsync", "3.3" },
        { "RemoveGroupFromGroup", "RtGroupMethodClient.RemoveChildGroupAsync", "3.3" },
        // Clients
        { "CreateClient", "RtClientMethodClient.CreateClientAsync", "3.8" },
        { "UpdateClient", "RtClientMethodClient.UpdateClientAsync", "3.8" },
        { "DeleteClient", "RtClientMethodClient.DeleteClientAsync", "3.8" },
        { "UpdateClientRoles", "RtClientMethodClient.SetRolesAsync", "3.3" },
        { "AddRoleToClient", "RtClientMethodClient.AddRoleAsync", "3.3" },
        { "RemoveRoleFromClient", "RtClientMethodClient.RemoveRoleAsync", "3.3" },
        { "ApplyClientOverlay", "RtClientMethodClient.ApplyOverlayUrisAsync", "3.3" },
        { "CleanOverlayEntries", "RtClientMethodClient.CleanOverlayEntriesAsync", "3.3" },
        { "CreateApiSecretForClient", "RtClientMethodClient.AddSecretAsync", "3.4" },
        { "UpdateApiSecretClient", "RtClientMethodClient.UpdateSecretAsync", "3.4" },
        { "DeleteApiSecretClient", "RtClientMethodClient.RemoveSecretAsync", "3.4" },
        { "ProvisionClientInExistingTenants", "RtClientMethodClient.ProvisionMirrorsAsync", "3.5" },
        { "ProvisionClientInTenant", "RtClientMethodClient.ProvisionMirrorInTenantAsync", "3.5" },
        { "UnprovisionClientFromTenant", "RtClientMethodClient.RemoveMirrorAsync", "3.5" },
        { "SetClientAutoProvisionInChildTenants", "RtClientMethodClient.SetMirrorAutoProvisionAsync", "3.5" },
        // Roles, API scopes, API resources
        { "CreateRole", "RtRoleMethodClient.CreateRoleAsync", "3.7" },
        { "UpdateRole", "RtRoleMethodClient.UpdateRoleAsync", "3.7" },
        { "DeleteRole", "RtRoleMethodClient.DeleteRoleAsync", "3.7" },
        { "CreateApiScope", "RtApiScopeMethodClient.CreateApiScopeAsync", "3.7" },
        { "UpdateApiScope", "RtApiScopeMethodClient.UpdateApiScopeAsync", "3.7" },
        { "DeleteScope", "RtApiScopeMethodClient.DeleteApiScopeAsync", "3.7" },
        { "CreateApiResource", "RtApiResourceMethodClient.CreateApiResourceAsync", "3.7" },
        { "UpdateApiResource", "RtApiResourceMethodClient.UpdateApiResourceAsync", "3.7" },
        { "DeleteApiResource", "RtApiResourceMethodClient.DeleteApiResourceAsync", "3.7" },
        { "CreateApiSecretForApiResource", "RtApiResourceMethodClient.AddSecretAsync", "3.4" },
        { "UpdateApiSecretApiResource", "RtApiResourceMethodClient.UpdateSecretAsync", "3.4" },
        { "DeleteApiSecretApiResource", "RtApiResourceMethodClient.RemoveSecretAsync", "3.4" },
        // Data permissions and policies
        { "GrantDataPermissionToRole", "RtDataPermissionMethodClient.GrantToRoleAsync", "3.4" },
        { "RevokeDataPermissionFromRole", "RtDataPermissionMethodClient.RevokeFromRoleAsync", "3.4" },
        { "SetDataPolicyEnforcementMode", "RtDataPolicyMethodClient.SetEnforcementModeAsync", "3.4" },
        { "CreateDataPermission", "RtDataPermissionMethodClient.CreateDataPermissionAsync", "3.8" },
        { "DeleteDataPermission", "RtDataPermissionMethodClient.DeleteDataPermissionAsync", "3.8" },
        { "CreateDataPolicy", "RtDataPermissionMethodClient.CreatePolicyAsync", "3.8" },
        { "DeleteDataPolicy", "RtDataPolicyMethodClient.DeleteDataPolicyAsync", "3.8" },
        // External tenant user mappings and admin provisioning
        { "CreateExternalTenantUserMapping", "RtExternalTenantUserMappingMethodClient.CreateMappingAsync", "3.5" },
        { "GrantExternalTenantUser", "RtExternalTenantUserMappingMethodClient.CreateMappingAsync", "3.5" },
        { "UpdateExternalTenantUserMapping", "RtExternalTenantUserMappingMethodClient.UpdateMappingAsync", "3.8" },
        { "DeleteExternalTenantUserMapping", "RtExternalTenantUserMappingMethodClient.DeleteMappingAsync", "3.8" },
        {
            "CreateAdminProvisioningMapping",
            "RtExternalTenantUserMappingMethodClient.ProvisionAdminAsync or ProvisionAdminWithGroupsAsync", "3.5"
        },
        { "ProvisionCurrentUser", "RtExternalTenantUserMappingMethodClient.ProvisionCurrentUserAsync", "3.5" },
        { "DeleteAdminProvisioningMapping", "RtExternalTenantUserMappingMethodClient.RemoveAdminProvisioningAsync", "3.5" },
        // Identity providers and e-mail domain group rules
        {
            "CreateIdentityProvider",
            "the method client of the concrete provider type, e.g. RtGoogleIdentityProviderMethodClient.CreateIdentityProviderAsync", "3.8"
        },
        {
            "UpdateIdentityProvider",
            "the method client of the concrete provider type, e.g. RtGoogleIdentityProviderMethodClient.UpdateIdentityProviderAsync", "3.8"
        },
        {
            "DeleteIdentityProvider",
            "the method client of the concrete provider type, e.g. RtGoogleIdentityProviderMethodClient.DeleteIdentityProviderAsync", "3.8"
        },
        { "CreateEmailDomainGroupRule", "RtEmailDomainGroupRuleMethodClient.CreateEmailDomainGroupRuleAsync", "3.8" },
        { "UpdateEmailDomainGroupRule", "RtEmailDomainGroupRuleMethodClient.UpdateEmailDomainGroupRuleAsync", "3.8" },
        { "DeleteEmailDomainGroupRule", "RtEmailDomainGroupRuleMethodClient.DeleteEmailDomainGroupRuleAsync", "3.8" }
    };

    [Theory]
    [MemberData(nameof(Wave2))]
    public void Wave2RestMethod_IsObsolete_WithTheReplacement(string method, string replacement, string minor)
    {
        foreach (var type in new[] { typeof(IIdentityServicesClient), typeof(IdentityServicesClient) })
        {
            var obsolete = type.GetMethod(method)!.GetCustomAttribute<ObsoleteAttribute>();

            Assert.NotNull(obsolete);
            Assert.False(obsolete.IsError, "the REST method keeps working during its REST window");
            Assert.Equal(
                $"Use {replacement} generated from System.Identity-[{minor},4.0) (construction-kits.yaml); the REST method keeps "
                + "working during its REST window. See docs/method-client-migration-guide.md (AB#9044).",
                obsolete.Message);
        }
    }

    [Fact]
    public void ExactlyTheMigratedMethods_AreObsolete()
    {
        // Wave 1 (UserRestMethodsObsoleteTests) plus wave 2; nothing else, so a read or an unmigrated write cannot turn
        // obsolete by accident.
        var expected = Wave2().Select(row => row.Data.Item1)
            .Concat(["AddRoleToUser", "CreateUser", "RemoveRoleFromUser", "ResetPassword"])
            .Order(StringComparer.Ordinal)
            .ToList();

        foreach (var type in new[] { typeof(IIdentityServicesClient), typeof(IdentityServicesClient) })
        {
            var obsolete = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<ObsoleteAttribute>() != null)
                .Select(m => m.Name)
                .Order(StringComparer.Ordinal)
                .ToList();

            Assert.Equal(expected, obsolete);
        }
    }

    [Theory]
    // Reads and writes without a CK method replacement in waves 1 and 2 stay usable without a warning.
    [InlineData("GetGroups")]
    [InlineData("GetClients")]
    [InlineData("GetApiSecretsForClient")]
    [InlineData("GetDataPermissions")]
    [InlineData("GetAdminProvisioningMappings")]
    [InlineData("GetCrossTenantGrantPermissions")]
    [InlineData("UpdateCrossTenantGrantPermissions")]
    [InlineData("LookupSourceUser")]
    [InlineData("UpdateUser")]
    [InlineData("DeleteUser")]
    [InlineData("RevokeUserSessions")]
    public void NotMigratedMethod_IsNotObsolete(string method)
    {
        Assert.Null(typeof(IIdentityServicesClient).GetMethod(method)!.GetCustomAttribute<ObsoleteAttribute>());
    }
}
