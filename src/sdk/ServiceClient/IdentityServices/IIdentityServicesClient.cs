using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Meshmakers.Octo.Common.Shared.DataTransferObjects;

namespace Meshmakers.Octo.Sdk.Client.IdentityServices;

public interface IIdentityServicesClient : IServiceClient
{
    Task<IEnumerable<IdentityProviderDto>> GetIdentityProviders();
    Task<IdentityProviderDto> GetIdentityProvider(string id);
    Task CreateIdentityProvider(IdentityProviderDto identityProvider);
    Task UpdateIdentityProvider(string id, IdentityProviderDto identityProvider);
    Task DeleteIdentityProvider(string id);
    Task<IEnumerable<ClientDto>> GetClients();
    Task<ClientDto> GetClient(string clientId);
    Task CreateClient(ClientDto client);
    Task UpdateClient(string clientId, ClientDto client);
    Task DeleteClient(string clientId);
    Task<IEnumerable<UserDto>> GetUsers();
    Task<UserDto> GetUserByNameEmailOrId(string userName);
    Task CreateUser(UserDto userDto);
    Task UpdateUser(string userName, UserDto userDto);
    Task DeleteUser(string userName);
    Task ResetPassword(string userName, string password);
    Task<RoleDto> GetRoleByName(string roleName);

    Task<IEnumerable<RoleDto>> GetRoles();
    Task CreateRole(RoleDto roleDto);
    Task UpdateRole(string roleName, RoleDto roleDto);
    Task DeleteRole(string roleName);
    Task<IEnumerable<ApiScopeDto>> GetApiScopes();
    Task<ApiScopeDto> GetApiScope(string name);
    Task DeleteScope(string name);
    Task UpdateApiScope(string scopeName, ApiScopeDto scopeDto);
    Task CreateApiScope(ApiScopeDto apiScopeDto);
    Task DeleteApiSecretClient(string clientId, string secretValue);
    Task DeleteApiSecretApiResource(string apiResourceName, string secretValue);
    Task<IEnumerable<ApiSecretDto>> GetApiSecretsForClient(string clientId);
    [ItemCanBeNull] Task<ApiSecretDto> GetApiSecretForClient(string clientId, string secretValue);
    Task<IEnumerable<ApiSecretDto>> GetApiSecretsForApiResource(string apiResourceName);
    [ItemCanBeNull] Task<ApiSecretDto> GetApiSecretForApiResource(string apiResourceName, string secretValue);
    Task<ApiSecretDto> CreateApiSecretForClient(string clientId, ApiSecretDto apiSecretDto);
    Task<ApiSecretDto> CreateApiSecretForApiResource(string apiResourceName, ApiSecretDto apiSecretDto);
    Task UpdateApiSecretClient(string clientId, ApiSecretDto apiSecretDto);
    Task UpdateApiSecretApiResource(string apiResourceName, ApiSecretDto apiSecretDto);
    Task AssignRoleToUser(string userId, string roleId);
    Task RemoveRoleFromUser(string userId, string roleId);
    Task<List<ApiResourceDto>> GetApiResources();
    Task CreateApiResource(ApiResourceDto apiResourceDto);
    Task DeleteApiResource(string name);
    Task UpdateApiResource(string resourceName, ApiResourceDto apiResourceDto);
}
