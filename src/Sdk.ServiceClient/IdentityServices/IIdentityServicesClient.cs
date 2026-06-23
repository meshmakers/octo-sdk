using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts;

namespace Meshmakers.Octo.Sdk.ServiceClient.IdentityServices;

/// <summary>
///     Interface for the identity services.
/// </summary>
public interface IIdentityServicesClient : IServiceClient
{
    /// <summary>
    ///     Returns the identity providers.
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<IdentityProviderDto>> GetIdentityProviders();

    /// <summary>
    ///     Gets an identity provider by id.
    /// </summary>
    /// <param name="rtId">The identifier of the identity provider</param>
    /// <returns></returns>
    Task<IdentityProviderDto> GetIdentityProvider(OctoObjectId rtId);

    /// <summary>
    ///     Creates an identity provider.
    /// </summary>
    /// <param name="identityProvider">The identity provider data transfer object</param>
    /// <returns></returns>
    Task CreateIdentityProvider(IdentityProviderDto identityProvider);

    /// <summary>
    ///     Updates an identity provider.
    /// </summary>
    /// <param name="rtId">The identifier of the identity provider</param>
    /// <param name="identityProvider">The identity provider data transfer object</param>
    /// <returns></returns>
    Task UpdateIdentityProvider(OctoObjectId rtId, IdentityProviderDto identityProvider);

    /// <summary>
    ///     Delete an identity provider.
    /// </summary>
    /// <param name="rtId">The identifier of the identity provider</param>
    /// <returns></returns>
    Task DeleteIdentityProvider(OctoObjectId rtId);

    /// <summary>
    ///     Gets a list of clients.
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<ClientDto>> GetClients();

    /// <summary>
    ///     Gets a client by id.
    /// </summary>
    /// <param name="clientId">The identifier of the client</param>
    /// <returns></returns>
    Task<ClientDto> GetClient(string clientId);

    /// <summary>
    ///     Creates a client.
    /// </summary>
    /// <param name="client">The client data transfer object</param>
    /// <returns></returns>
    Task CreateClient(ClientDto client);

    /// <summary>
    ///     Updates a client.
    /// </summary>
    /// <param name="clientId">The identifier of the client</param>
    /// <param name="client">The client data transfer object</param>
    /// <returns></returns>
    Task UpdateClient(string clientId, ClientDto client);

    /// <summary>
    ///     Deletes a client.
    /// </summary>
    /// <param name="clientId">The identifier of the client</param>
    /// <returns></returns>
    Task DeleteClient(string clientId);

    /// <summary>
    ///     Gets a list of users.
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<UserDto>> GetUsers();

    /// <summary>
    ///     Gets a user by name or email.
    /// </summary>
    /// <param name="userNameOrEMailAddress">User name or e-mail of user</param>
    /// <returns></returns>
    Task<UserDto> GetUserByNameEmailOrId(string userNameOrEMailAddress);

    /// <summary>
    ///     Creates a user.
    /// </summary>
    /// <param name="userDto">User data transfer object</param>
    /// <returns></returns>
    Task CreateUser(UserDto userDto);

    /// <summary>
    ///     Updates a user.
    /// </summary>
    /// <param name="userNameOrEMailAddress">User name or e-mail of user</param>
    /// <param name="userDto">User data transfer object</param>
    /// <returns></returns>
    Task UpdateUser(string userNameOrEMailAddress, UserDto userDto);

    /// <summary>
    ///     Deletes a user.
    /// </summary>
    /// <param name="userNameOrEMailAddress">User name or e-mail of user</param>
    /// <returns></returns>
    Task DeleteUser(string userNameOrEMailAddress);

    /// <summary>
    ///     Resets the password of a user.
    /// </summary>
    /// <param name="userNameOrEMailAddress">User name or e-mail of user</param>
    /// <param name="password">New password</param>
    /// <returns></returns>
    Task ResetPassword(string userNameOrEMailAddress, string password);
    
    /// <summary>
    ///     Gets a role by name.
    /// </summary>
    /// <param name="roleName">Name of role</param>
    /// <returns></returns>
    Task<RoleDto> GetRoleByName(string roleName);

    /// <summary>
    ///     Get a list of roles.
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<RoleDto>> GetRoles();

    /// <summary>
    ///     Creates a role
    /// </summary>
    /// <param name="roleDto">Role data transfer object</param>
    /// <returns></returns>
    Task CreateRole(RoleDto roleDto);

    /// <summary>
    ///     Updates a role.
    /// </summary>
    /// <param name="roleName">Name of role</param>
    /// <param name="roleDto">Role data transfer object</param>
    /// <returns></returns>
    Task UpdateRole(string roleName, RoleDto roleDto);

    /// <summary>
    ///     Deletes a role.
    /// </summary>
    /// <param name="roleName">Name of role</param>
    /// <returns></returns>
    Task DeleteRole(string roleName);

    /// <summary>
    ///     Gets a list of scopes
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<ApiScopeDto>> GetApiScopes();

    /// <summary>
    ///     Gets an api scope by name.
    /// </summary>
    /// <param name="name">Name of scope</param>
    /// <returns></returns>
    Task<ApiScopeDto> GetApiScope(string name);

    /// <summary>
    ///     Deletes a scope.
    /// </summary>
    /// <param name="name">Name of scope</param>
    /// <returns></returns>
    Task DeleteScope(string name);

    /// <summary>
    ///     Updates a scope
    /// </summary>
    /// <param name="name">Name of scope</param>
    /// <param name="scopeDto">Scope data transfer object</param>
    /// <returns></returns>
    Task UpdateApiScope(string name, ApiScopeDto scopeDto);

    /// <summary>
    ///     Creates a scope.
    /// </summary>
    /// <param name="apiScopeDto">Scope data transfer object</param>
    /// <returns></returns>
    Task CreateApiScope(ApiScopeDto apiScopeDto);

    /// <summary>
    ///     Deletes an api secret of a client.
    /// </summary>
    /// <param name="clientId">The client id of the secret</param>
    /// <param name="secretValue">The secret value</param>
    /// <returns></returns>
    Task DeleteApiSecretClient(string clientId, string secretValue);

    /// <summary>
    ///     Deletes an api secret of a api resource.
    /// </summary>
    /// <param name="apiResourceName">The name of the API resource</param>
    /// <param name="secretValue">The secret value</param>
    /// <returns></returns>
    Task DeleteApiSecretApiResource(string apiResourceName, string secretValue);

    /// <summary>
    ///     Gets a list of api secrets for a client.
    /// </summary>
    /// <param name="clientId"></param>
    /// <returns></returns>
    Task<IEnumerable<ApiSecretDto>> GetApiSecretsForClient(string clientId);

    /// <summary>
    ///     Gets a api secret for a client.
    /// </summary>
    /// <param name="clientId">The identifier of the client</param>
    /// <param name="secretValue">The secret value</param>
    /// <returns></returns>
    Task<ApiSecretDto> GetApiSecretForClient(string clientId, string secretValue);

    /// <summary>
    ///     Gets a list of api secrets for a api resource.
    /// </summary>
    /// <param name="apiResourceName">Name of API resource</param>
    /// <returns></returns>
    Task<IEnumerable<ApiSecretDto>> GetApiSecretsForApiResource(string apiResourceName);

    /// <summary>
    ///     Gets a api secret for a api resource.
    /// </summary>
    /// <param name="apiResourceName">Name of API resource</param>
    /// <param name="secretValue">The secret value</param>
    /// <returns></returns>
    Task<ApiSecretDto> GetApiSecretForApiResource(string apiResourceName, string secretValue);

    /// <summary>
    ///     Creates an api secret for a client.
    /// </summary>
    /// <param name="clientId">The client id</param>
    /// <param name="apiSecretDto">The API secret data transfer object</param>
    /// <returns></returns>
    Task<ApiSecretDto> CreateApiSecretForClient(string clientId, ApiSecretDto apiSecretDto);

    /// <summary>
    ///     Creates an api secret for a api resource.
    /// </summary>
    /// <param name="apiResourceName">Name of API resource</param>
    /// <param name="apiSecretDto">The secret data transfer object</param>
    /// <returns></returns>
    Task<ApiSecretDto> CreateApiSecretForApiResource(string apiResourceName, ApiSecretDto apiSecretDto);

    /// <summary>
    ///     Updates an api secret of a client.
    /// </summary>
    /// <param name="clientId">The client id</param>
    /// <param name="apiSecretDto">The secret data transfer object</param>
    /// <returns></returns>
    Task UpdateApiSecretClient(string clientId, ApiSecretDto apiSecretDto);

    /// <summary>
    ///     Updates an api secret of a api resource.
    /// </summary>
    /// <param name="apiResourceName">Name of API resource</param>
    /// <param name="apiSecretDto">API secret data transfer object</param>
    /// <returns></returns>
    Task UpdateApiSecretApiResource(string apiResourceName, ApiSecretDto apiSecretDto);

    /// <summary>
    ///     Adds a role to a user.
    /// </summary>
    /// <param name="userNameOrEMailAddress">User name or E-Mail address of the user</param>
    /// <param name="roleId">The id of the role</param>
    /// <returns></returns>
    Task AddRoleToUser(string userNameOrEMailAddress, string roleId);

    /// <summary>
    ///     Removes a role from a user.
    /// </summary>
    /// <param name="userNameOrEMailAddress">User name or E-Mail address of the user</param>
    /// <param name="roleId">The id of the role</param>
    /// <returns></returns>
    Task RemoveRoleFromUser(string userNameOrEMailAddress, string roleId);

    /// <summary>
    ///     Gets a list of API resources.
    /// </summary>
    /// <returns></returns>
    Task<List<ApiResourceDto>> GetApiResources();

    /// <summary>
    ///     Creates an API resource.
    /// </summary>
    /// <param name="apiResourceDto">API resource data transfer object</param>
    /// <returns></returns>
    Task CreateApiResource(ApiResourceDto apiResourceDto);

    /// <summary>
    ///     Deletes an API resource.
    /// </summary>
    /// <param name="name">Name of API resource</param>
    /// <returns></returns>
    Task DeleteApiResource(string name);

    /// <summary>
    ///     Updates an API resource.
    /// </summary>
    /// <param name="name">Name of API resource</param>
    /// <param name="apiResourceDto">API resource data transfer object</param>
    /// <returns></returns>
    Task UpdateApiResource(string name, ApiResourceDto apiResourceDto);
    
    /// <summary>
    ///     Gets all email domain group rules.
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<EmailDomainGroupRuleDto>> GetEmailDomainGroupRules();

    /// <summary>
    ///     Gets an email domain group rule by id.
    /// </summary>
    /// <param name="rtId">The identifier of the rule</param>
    /// <returns></returns>
    Task<EmailDomainGroupRuleDto> GetEmailDomainGroupRule(OctoObjectId rtId);

    /// <summary>
    ///     Creates an email domain group rule.
    /// </summary>
    /// <param name="rule">The rule data transfer object</param>
    /// <returns></returns>
    Task CreateEmailDomainGroupRule(EmailDomainGroupRuleDto rule);

    /// <summary>
    ///     Updates an email domain group rule.
    /// </summary>
    /// <param name="rtId">The identifier of the rule</param>
    /// <param name="rule">The rule data transfer object</param>
    /// <returns></returns>
    Task UpdateEmailDomainGroupRule(OctoObjectId rtId, EmailDomainGroupRuleDto rule);

    /// <summary>
    ///     Deletes an email domain group rule.
    /// </summary>
    /// <param name="rtId">The identifier of the rule</param>
    /// <returns></returns>
    Task DeleteEmailDomainGroupRule(OctoObjectId rtId);

    /// <summary>
    ///     Gets all groups.
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<GroupDto>> GetGroups();

    /// <summary>
    ///     Gets a group by id.
    /// </summary>
    /// <param name="rtId">The identifier of the group</param>
    /// <returns></returns>
    Task<GroupDto> GetGroup(OctoObjectId rtId);

    /// <summary>
    ///     Gets a group by name.
    /// </summary>
    /// <param name="name">The name of the group</param>
    /// <returns></returns>
    Task<GroupDto> GetGroupByName(string name);

    /// <summary>
    ///     Creates a group.
    /// </summary>
    /// <param name="group">The group data transfer object</param>
    /// <returns></returns>
    Task CreateGroup(CreateGroupDto group);

    /// <summary>
    ///     Updates a group.
    /// </summary>
    /// <param name="rtId">The identifier of the group</param>
    /// <param name="group">The group data transfer object</param>
    /// <returns></returns>
    Task UpdateGroup(OctoObjectId rtId, UpdateGroupDto group);

    /// <summary>
    ///     Deletes a group.
    /// </summary>
    /// <param name="rtId">The identifier of the group</param>
    /// <returns></returns>
    Task DeleteGroup(OctoObjectId rtId);

    /// <summary>
    ///     Updates the roles assigned to a group.
    /// </summary>
    /// <param name="rtId">The identifier of the group</param>
    /// <param name="roleIds">The list of role IDs to assign</param>
    /// <returns></returns>
    Task UpdateGroupRoles(OctoObjectId rtId, List<string> roleIds);

    /// <summary>
    ///     Adds a user to a group.
    /// </summary>
    /// <param name="rtId">The identifier of the group</param>
    /// <param name="userId">The identifier of the user</param>
    /// <returns></returns>
    Task AddUserToGroup(OctoObjectId rtId, string userId);

    /// <summary>
    ///     Removes a user from a group.
    /// </summary>
    /// <param name="rtId">The identifier of the group</param>
    /// <param name="userId">The identifier of the user</param>
    /// <returns></returns>
    Task RemoveUserFromGroup(OctoObjectId rtId, string userId);

    /// <summary>
    ///     Adds a child group to a parent group.
    /// </summary>
    /// <param name="rtId">The identifier of the parent group</param>
    /// <param name="childGroupId">The identifier of the child group</param>
    /// <returns></returns>
    Task AddGroupToGroup(OctoObjectId rtId, string childGroupId);

    /// <summary>
    ///     Removes a child group from a parent group.
    /// </summary>
    /// <param name="rtId">The identifier of the parent group</param>
    /// <param name="childGroupId">The identifier of the child group</param>
    /// <returns></returns>
    Task RemoveGroupFromGroup(OctoObjectId rtId, string childGroupId);

    /// <summary>
    ///     Gets external tenant user mappings.
    /// </summary>
    /// <param name="skip">Number of items to skip</param>
    /// <param name="take">Number of items to take</param>
    /// <param name="sourceTenantId">Optional source tenant ID to filter by</param>
    /// <returns></returns>
    Task<IEnumerable<ExternalTenantUserMappingDto>> GetExternalTenantUserMappings(int? skip = null, int? take = null,
        string? sourceTenantId = null);

    /// <summary>
    ///     Gets an external tenant user mapping by id.
    /// </summary>
    /// <param name="rtId">The identifier of the mapping</param>
    /// <returns></returns>
    Task<ExternalTenantUserMappingDto> GetExternalTenantUserMapping(OctoObjectId rtId);

    /// <summary>
    ///     Creates an external tenant user mapping.
    /// </summary>
    /// <param name="mapping">The mapping data transfer object</param>
    /// <returns></returns>
    Task CreateExternalTenantUserMapping(CreateExternalTenantUserMappingDto mapping);

    /// <summary>
    ///     Updates an external tenant user mapping.
    /// </summary>
    /// <param name="rtId">The identifier of the mapping</param>
    /// <param name="mapping">The mapping data transfer object</param>
    /// <returns></returns>
    Task UpdateExternalTenantUserMapping(OctoObjectId rtId, UpdateExternalTenantUserMappingDto mapping);

    /// <summary>
    ///     Deletes an external tenant user mapping.
    /// </summary>
    /// <param name="rtId">The identifier of the mapping</param>
    /// <returns></returns>
    Task DeleteExternalTenantUserMapping(OctoObjectId rtId);

    /// <summary>
    ///     Gets admin provisioning mappings for a target tenant.
    /// </summary>
    /// <param name="targetTenantId">The target tenant ID</param>
    /// <returns></returns>
    Task<IEnumerable<ExternalTenantUserMappingDto>> GetAdminProvisioningMappings(string targetTenantId);

    /// <summary>
    ///     Creates an admin provisioning mapping in a target tenant.
    /// </summary>
    /// <param name="targetTenantId">The target tenant ID</param>
    /// <param name="mapping">The mapping data transfer object</param>
    /// <returns></returns>
    Task CreateAdminProvisioningMapping(string targetTenantId, CreateExternalTenantUserMappingDto mapping);

    /// <summary>
    ///     Provisions the current user in a target tenant.
    /// </summary>
    /// <param name="targetTenantId">The target tenant ID</param>
    /// <returns></returns>
    Task ProvisionCurrentUser(string targetTenantId);

    /// <summary>
    ///     Deletes an admin provisioning mapping from a target tenant.
    /// </summary>
    /// <param name="targetTenantId">The target tenant ID</param>
    /// <param name="mappingRtId">The identifier of the mapping</param>
    /// <returns></returns>
    Task DeleteAdminProvisioningMapping(string targetTenantId, OctoObjectId mappingRtId);

    /// <summary>
    ///     Reconfigure the log level of the service.
    /// </summary>
    /// <param name="loggerName">Logger pattern name, e. g. Microsoft.*</param>
    /// <param name="minLogLevel">Minimal log level to be logged.</param>
    /// <param name="maxLogLevel">Maximum log level to be logged.</param>
    /// <returns></returns>
    Task ReconfigureLogLevelAsync(string loggerName, LogLevelDto minLogLevel, LogLevelDto maxLogLevel);

    /// <summary>
    ///     Lists the child tenants a ClientCredentials client has been
    ///     auto-provisioned into.
    /// </summary>
    Task<IEnumerable<ClientMirrorDto>> GetClientMirrors(string clientId);

    /// <summary>
    ///     Backfill: provisions this client into every existing child tenant of the
    ///     caller. Server returns <c>400</c> if the client isn't flagged with
    ///     <see cref="ClientDto.AutoProvisionInChildTenants"/>=true.
    /// </summary>
    Task<ClientMirrorBackfillResponseDto> ProvisionClientInExistingTenants(string clientId);

    /// <summary>
    ///     Provisions this client into a single named child tenant.
    /// </summary>
    Task<ClientMirrorProvisionResponseDto> ProvisionClientInTenant(string clientId, string childTenantId);

    /// <summary>
    ///     Removes a single mirror (drops both the child-side <c>RtClient</c> and
    ///     the parent's tracking row).
    /// </summary>
    Task UnprovisionClientFromTenant(string clientId, string childTenantId);

    /// <summary>
    ///     Flips the <see cref="ClientDto.AutoProvisionInChildTenants"/> flag on a
    ///     client without rewriting the full client object.
    /// </summary>
    Task SetClientAutoProvisionInChildTenants(string clientId, bool enabled);

    /// <summary>
    ///     Applies an overlay URI set to a client (AB#4209 Step 4). The endpoint dedupes
    ///     each incoming URI against the existing list contents and appends new entries
    ///     with <c>Source = "overlay:&lt;OverlayName&gt;"</c>. Idempotent — re-running with
    ///     the same payload is a no-op (no DB write, no cache invalidation). Returns
    ///     per-list <c>(Added, SkippedDuplicate)</c> counts.
    /// </summary>
    Task<ApplyOverlayUrisResultDto> ApplyClientOverlay(string clientId, ApplyOverlayUrisDto dto);

    /// <summary>
    ///     Strips overlay URI entries from every blueprint-managed client in the tenant
    ///     (AB#4209 Step 5). Without <paramref name="overlayName"/>: strips every entry
    ///     where <c>Source</c> starts with <c>overlay:</c>. With <paramref name="overlayName"/>:
    ///     strips only entries where <c>Source</c> matches <c>overlay:&lt;overlayName&gt;</c>
    ///     exactly. Idempotent — clients with nothing to remove skip the per-client
    ///     <c>UpdateAsync</c> + cache invalidation. Returns per-client and total counts.
    /// </summary>
    Task<CleanOverlayEntriesResultDto> CleanOverlayEntries(string? overlayName);
}