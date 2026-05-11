namespace Meshmakers.Octo.Communication.Contracts;

/// <summary>
///     Common constants used in the application
/// </summary>
public static class CommonConstants
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    public const string GoogleIdentityProvider = "Google";
    public const string MicrosoftIdentityProvider = "Microsoft";

    public const string OctoApi = "octoAPI";
    public const string OctoApiDisplayName = "Octo API";
    public const string OctoApiDescription = "Unified access to all Octo platform APIs";
    public const string OctoApiFullAccess = "octo_api";
    public const string OctoApiFullAccessDisplayName = "Full access to all Octo platform APIs";
    public const string OctoApiReadOnly = "octo_api.read_only";
    public const string OctoApiReadOnlyDisplayName = "Read-only access to all Octo platform APIs";
    public const string OctoApiDataModelManagement = "octo_api.data_model_management";
    public const string OctoApiDataModelManagementDisplayName = "Manage construction kit data models";

    public const string OctoToolClientId = "octo-cli";
    public const string OctoToolClientSecret = "{AEE2DA50-065E-4071-A56E-7B3B4B175EFB}";

    public const string OctoAdminPanelClientId = "octo-admin-panel";
    public const string OctoAdminPanelClientIdDebug = "octo-admin-panel-debug";

    public const string AssetRepositoryServicesClientId = "octo-assetRepositoryServices";
    public const string BotServicesClientId = "octo-botServices";
    public const string CommunicationControllerServicesClientId = "octo-communicationControllerServices";
    public const string ReportingServicesClientId = "octo-reportingServices";

    public const string IdentityServicesSwaggerClientId = "octo-idenityServices-swagger";
    public const string AsserRepositoryServicesSwaggerClientId = "octo-assetRepositoryServices-swagger";
    public const string BotServicesSwaggerClientId = "octo-botServices-swagger";
    public const string CommunicationControllerServicesSwaggerClientId = "octo-communicationControllerServices-swagger";
    public const string ReportingServicesSwaggerClientId = "octo-reportingServices-swagger";

    public const string McpServicesSwaggerClientId = "octo-mcpServices-swagger";
    public const string McpServicesDeviceClientId = "octo-mcpServices-device";

    public const string AdminPanelManagementRole = "AdminPanelManagement";
    public const string BotManagementRole = "BotManagement";
    public const string TenantManagementRole = "TenantManagement";
    public const string DevelopmentRole = "Development";
    public const string UserManagementRole = "UserManagement";
    public const string CommunicationManagementRole = "CommunicationManagement";
    public const string DashboardManagementRole = "DashboardManagement";
    public const string DashboardViewerRole = "DashboardViewer";
    public const string ReportingManagementRole = "ReportingManagement";
    public const string ReportingViewerRole = "ReportingViewer";
    public const string DataModelManagementRole = "DataModelManagement";
    public const string StreamDataAdminRole = "StreamDataAdmin";
    public const string StreamDataWriterRole = "StreamDataWriter";
    public const string StreamDataReaderRole = "StreamDataReader";

    public const string TenantOwnersGroup = "TenantOwners";

    /// <summary>
    ///     Defines default scopes as minimal constraint
    /// </summary>
    public static readonly string[] OctoDefaultScopes =
    [
        Scopes.OpenId,
        Scopes.Profile,
        Scopes.Email,
        Scopes.Role
    ];

    /// <summary>
    ///     Returns a scope definition including default scopes and api scopes
    /// </summary>
    /// <param name="apiScopes">Enum flags for API scopes.</param>
    /// <param name="customScopes">Additional custom scopes to be added to the token</param>
    /// <param name="scopes">Default scopes that are added </param>
    /// <returns></returns>
    public static string GetScopes(ApiScopes apiScopes, IEnumerable<string>? customScopes = null,
        DefaultScopes scopes = DefaultScopes.UserDefault)
    {
        var list = GetDefaultScopes(scopes);

        if (apiScopes.HasFlag(ApiScopes.OctoApiFullAccess))
        {
            list.Add(OctoApiFullAccess);
        }
        else if (apiScopes.HasFlag(ApiScopes.OctoApiReadOnly))
        {
            list.Add(OctoApiReadOnly);
        }

        if (customScopes != null)
        {
            foreach (var customScope in customScopes)
            {
                if (list.All(s => string.Compare(s, customScope, StringComparison.OrdinalIgnoreCase) != 0))
                {
                    list.Add(customScope);
                }
            }
        }

        return string.Join(" ", list.ToArray());
    }

    private static List<string> GetDefaultScopes(DefaultScopes scopes)
    {
        var list = new List<string>();
        if (scopes.HasFlag(DefaultScopes.OpenId))
        {
            list.Add(Scopes.OpenId);
        }

        if (scopes.HasFlag(DefaultScopes.Profile))
        {
            list.Add(Scopes.Profile);
        }

        if (scopes.HasFlag(DefaultScopes.Email))
        {
            list.Add(Scopes.Email);
        }

        if (scopes.HasFlag(DefaultScopes.Role))
        {
            list.Add(Scopes.Role);
        }

        if (scopes.HasFlag(DefaultScopes.OfflineAccess))
        {
            list.Add(Scopes.OfflineAccess);
        }

        return list;
    }


    /// <summary>
    ///     Defines standard scopes
    /// </summary>
    public static class Scopes
    {
        /// <summary>
        ///     REQUIRED. Informs the Authorization Server that the Client is making an OpenID Connect request. If the
        ///     <c>openid</c> scope value is not present, the behavior is entirely unspecified.
        /// </summary>
        public const string OpenId = "openid";

        /// <summary>
        ///     OPTIONAL. This scope value requests access to the End-User's default profile Claims, which are: <c>name</c>,
        ///     <c>family_name</c>, <c>given_name</c>, <c>middle_name</c>, <c>nickname</c>, <c>preferred_username</c>,
        ///     <c>profile</c>, <c>picture</c>, <c>website</c>, <c>gender</c>, <c>birthdate</c>, <c>zoneinfo</c>, <c>locale</c>,
        ///     and <c>updated_at</c>.
        /// </summary>
        public const string Profile = "profile";

        /// <summary>OPTIONAL. This scope value requests access to the <c>email</c> and <c>email_verified</c> Claims.</summary>
        public const string Email = "email";

        public const string Role = "role";
        public const string Permission = "permission";
        public const string OfflineAccess = "offline_access";
    }

    public static class PermissionIds
    {
        public const string PermissionRead = "policy.permission.read";
        public const string PermissionWrite = "policy.permission.write";
        public const string PermissionRoleRead = "policy.permissionRole.read";
        public const string PermissionRoleWrite = "policy.permissionRole.write";
    }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
