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
    ///     Reconfigure the log level of the service.
    /// </summary>
    /// <param name="minLogLevel">Minimal log level to be logged.</param>
    /// <returns></returns>
    Task ReconfigureLogLevelAsync(LogLevelDto minLogLevel);
}