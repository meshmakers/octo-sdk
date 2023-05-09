using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Meshmakers.Common.Shared;
using Meshmakers.Octo.Common.Shared;
using Meshmakers.Octo.Common.Shared.DataTransferObjects;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Meshmakers.Octo.Frontend.Client.System;

public class IdentityServicesClient : ServiceClient, IIdentityServicesClient
{
    public IdentityServicesClient(IOptions<IdentityServiceClientOptions> identityServiceClientOptions,
        IIdentityServiceClientAccessToken identityAccessToken)
        : this(identityServiceClientOptions.Value, identityAccessToken)
    {
    }

    public IdentityServicesClient(IdentityServiceClientOptions identityServiceClientOptions,
        IIdentityServiceClientAccessToken identityAccessToken)
        : base(identityServiceClientOptions, identityAccessToken)
    {
    }

    public async Task<IEnumerable<IdentityProviderDto>> GetIdentityProviders()
    {
        var request = new RestRequest("identityProviders");

        var response = await Client.ExecuteAsync<IdentityProvidersResult>(request);
        ValidateResponse(response);

        return response.Data?.IdentityProviders ?? new List<IdentityProviderDto>();
    }

    public async Task<IdentityProviderDto> GetIdentityProvider(string id)
    {
        ArgumentValidation.ValidateString(nameof(id), id);

        var request = new RestRequest("identityProviders/{id}");
        request.AddUrlSegment("id", id);

        var response = await Client.ExecuteAsync<IdentityProviderDto>(request);
        ValidateResponse(response);

        return response.Data!;
    }

    public async Task CreateIdentityProvider(IdentityProviderDto identityProvider)
    {
        var request = new RestRequest("identityProviders", Method.Post);
        request.AddJsonBody(identityProvider);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    public async Task UpdateIdentityProvider(string id, IdentityProviderDto identityProvider)
    {
        ArgumentValidation.ValidateString(nameof(id), id);

        var request = new RestRequest("identityProviders/{id}", Method.Put);
        request.AddUrlSegment("id", id);
        request.AddJsonBody(identityProvider);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    public async Task DeleteIdentityProvider(string id)
    {
        ArgumentValidation.ValidateString(nameof(id), id);

        var request = new RestRequest("identityProviders/{id}", Method.Delete);
        request.AddUrlSegment("id", id);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }


    public async Task<IEnumerable<ClientDto>> GetClients()
    {
        var request = new RestRequest("clients");

        var response = await Client.ExecuteAsync<List<ClientDto>>(request);
        ValidateResponse(response);

        return response.Data ?? new List<ClientDto>();
    }

    public async Task<ClientDto> GetClient(string clientId)
    {
        ArgumentValidation.ValidateString(nameof(clientId), clientId);

        var request = new RestRequest("clients/{id}");
        request.AddUrlSegment("id", clientId);

        var response = await Client.ExecuteAsync<ClientDto>(request);
        ValidateResponse(response);

        return response.Data!;
    }

    public async Task CreateClient(ClientDto client)
    {
        var request = new RestRequest("clients", Method.Post);
        request.AddJsonBody(client);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    public async Task UpdateClient(string clientId, ClientDto client)
    {
        ArgumentValidation.ValidateString(nameof(clientId), clientId);

        var request = new RestRequest("clients/{id}", Method.Put);
        request.AddUrlSegment("id", clientId);
        request.AddJsonBody(client);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    public async Task DeleteClient(string clientId)
    {
        ArgumentValidation.ValidateString(nameof(clientId), clientId);

        var request = new RestRequest("clients/{id}", Method.Delete);
        request.AddUrlSegment("id", clientId);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    public async Task<IEnumerable<UserDto>> GetUsers()
    {
        var request = new RestRequest("identities");

        var response = await Client.ExecuteAsync<List<UserDto>>(request);
        ValidateResponse(response);

        return response.Data ?? new List<UserDto>();
    }

    public async Task<UserDto> GetUserByNameEmailOrId(string userName)
    {
        var request = new RestRequest($"identities/{userName}");

        var response = await Client.ExecuteAsync<UserDto>(request);
        ValidateResponse(response);

        return response.Data!;
    }

    public async Task AssignRoleToUser(string userId, string roleId)
    {
        var request = new RestRequest($"identities/{userId}/roles/{roleId}", Method.Put);

        var response = await Client.ExecuteAsync(request);

        ValidateResponse(response);
    }
    
    public async Task RemoveRoleFromUser(string userId, string roleId)
    {
        var request = new RestRequest($"identities/{userId}/roles/{roleId}", Method.Delete);

        var response = await Client.ExecuteAsync(request);

        ValidateResponse(response);
    }

    public async Task CreateUser(UserDto userDto)
    {
        var request = new RestRequest("identities", Method.Post);
        request.AddJsonBody(userDto);

        var response = await Client.ExecutePostAsync(request);
        ValidateResponse(response);
    }

    public async Task UpdateUser(string userName, UserDto userDto)
    {
        ArgumentValidation.ValidateString(nameof(userName), userName);

        var request = new RestRequest("identities/{userName}", Method.Put);
        request.AddUrlSegment("userName", userName);
        request.AddJsonBody(userDto);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    public async Task DeleteUser(string userName)
    {
        ArgumentValidation.ValidateString(nameof(userName), userName);

        var request = new RestRequest("identities/{userName}", Method.Delete);
        request.AddUrlSegment("userName", userName);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    public async Task ResetPassword(string userName, string password)
    {
        ArgumentValidation.ValidateString(nameof(userName), userName);
        ArgumentValidation.ValidateString(nameof(password), password);

        var request = new RestRequest("identities/resetPassword", Method.Post);
        request.AddQueryParameter("userName", userName);
        request.AddQueryParameter("password", password);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    public async Task<RoleDto> GetRoleByName(string roleName)
    {
        ArgumentValidation.ValidateString(nameof(roleName), roleName);

        var request = new RestRequest("roles/names/{roleName}");
        request.AddUrlSegment("roleName", roleName);

        var response = await Client.ExecuteAsync<RoleDto>(request);
        ValidateResponse(response);

        return response.Data!;
    }

    public async Task<IEnumerable<RoleDto>> GetRoles()
    {
        var request = new RestRequest("roles");

        var response = await Client.ExecuteAsync<List<RoleDto>>(request);
        ValidateResponse(response);

        return response.Data ?? new List<RoleDto>(); 
    }

    public async Task CreateRole(RoleDto roleDto)
    {
        var request = new RestRequest("roles", Method.Post);
        request.AddJsonBody(roleDto);

        var response = await Client.ExecutePostAsync(request);
        ValidateResponse(response);
    }

    public async Task UpdateRole(string roleName, RoleDto roleDto)
    {
        ArgumentValidation.ValidateString(nameof(roleName), roleName);

        var request = new RestRequest("roles/{roleName}", Method.Put);
        request.AddUrlSegment("roleName", roleName);
        request.AddJsonBody(roleDto);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    public async Task DeleteRole(string roleName)
    {
        ArgumentValidation.ValidateString(nameof(roleName), roleName);

        var request = new RestRequest("roles/{roleName}", Method.Delete);
        request.AddUrlSegment("roleName", roleName);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    public async Task<IEnumerable<ApiScopeDto>> GetApiScopes()
    {
        var request = new RestRequest("apiScopes");

        var response = await Client.ExecuteAsync<List<ApiScopeDto>>(request);
        ValidateResponse(response);

        return response.Data ?? new List<ApiScopeDto>(); 
    }

    public async Task<ApiScopeDto> GetApiScope(string name)
    {
        ArgumentValidation.ValidateString(nameof(name), name);

        var request = new RestRequest("apiScopes/{name}");
        request.AddUrlSegment("name", name);

        var response = await Client.ExecuteAsync<ApiScopeDto>(request);
        ValidateResponse(response);

        return response.Data!;
    }

    public async Task DeleteScope(string name)
    {
        ArgumentValidation.ValidateString(nameof(name), name);

        var request = new RestRequest("apiScopes/{name}", Method.Delete);
        request.AddUrlSegment("name", name);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    public async Task UpdateApiScope(string scopeName, ApiScopeDto scopeDto)
    {
        ArgumentValidation.ValidateString(nameof(scopeName), scopeName);

        var request = new RestRequest("apiScopes/{name}", Method.Put);
        request.AddUrlSegment("name", scopeName);
        request.AddJsonBody(scopeDto);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    public async Task CreateApiScope(ApiScopeDto apiScopeDto)
    {
        var request = new RestRequest("apiScopes", Method.Post);
        request.AddJsonBody(apiScopeDto);

        var response = await Client.ExecutePostAsync(request);
        ValidateResponse(response);
    }

    public async Task DeleteApiSecretClient(string clientId, string secretValue)
    {
        ArgumentValidation.ValidateString(nameof(clientId), clientId);
        ArgumentValidation.ValidateString(nameof(secretValue), secretValue);

        var request = new RestRequest("apiSecrets/client/{clientId}/{value}", Method.Delete);
        request.AddUrlSegment("clientId", clientId);
        request.AddUrlSegment("value", secretValue.EncodeBase64());

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    public async Task DeleteApiSecretApiResource(string apiResourceName, string secretValue)
    {
        ArgumentValidation.ValidateString(nameof(apiResourceName), apiResourceName);
        ArgumentValidation.ValidateString(nameof(secretValue), secretValue.EncodeBase64());

        var request = new RestRequest("apiSecrets/apiResource/{apiResourceName}/{value}", Method.Delete);
        request.AddUrlSegment("apiResourceName", apiResourceName);
        request.AddUrlSegment("value", secretValue.EncodeBase64());

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    public async Task<IEnumerable<ApiSecretDto>> GetApiSecretsForClient(string clientId)
    {
        ArgumentValidation.ValidateString(nameof(clientId), clientId);

        var request = new RestRequest("apiSecrets/client/{clientId}");
        request.AddUrlSegment("clientId", clientId);

        var response = await Client.ExecuteAsync<List<ApiSecretDto>>(request);
        ValidateResponse(response);

        return response.Data ?? new List<ApiSecretDto>(); 
    }

    public async Task<ApiSecretDto> GetApiSecretForClient(string clientId, string secretValue)
    {
        ArgumentValidation.ValidateString(nameof(clientId), clientId);
        ArgumentValidation.ValidateString(nameof(secretValue), secretValue);

        var request = new RestRequest("apiSecrets/client/{clientId}/{value}");
        request.AddUrlSegment("clientId", clientId);
        request.AddUrlSegment("value", secretValue.EncodeBase64());

        var response = await Client.ExecuteAsync<ApiSecretDto>(request);
        ValidateResponse(response);

        return response.Data!; 
    }

    public async Task<IEnumerable<ApiSecretDto>> GetApiSecretsForApiResource(string apiResourceName)
    {
        ArgumentValidation.ValidateString(nameof(apiResourceName), apiResourceName);

        var request = new RestRequest("apiSecrets/apiResource/{apiResourceName}");
        request.AddUrlSegment("apiResourceName", apiResourceName);

        var response = await Client.ExecuteAsync<List<ApiSecretDto>>(request);
        ValidateResponse(response);

        return response.Data ?? new List<ApiSecretDto>(); 
    }

    public async Task<ApiSecretDto> GetApiSecretForApiResource(string apiResourceName, string secretValue)
    {
        ArgumentValidation.ValidateString(nameof(apiResourceName), apiResourceName);
        ArgumentValidation.ValidateString(nameof(secretValue), secretValue);

        var request = new RestRequest("apiSecrets/apiResource/{apiResourceName}/{value}");
        request.AddUrlSegment("apiResourceName", apiResourceName);
        request.AddUrlSegment("value", secretValue.EncodeBase64());

        var response = await Client.ExecuteAsync<ApiSecretDto>(request);
        ValidateResponse(response);

        return response.Data!; 
    }

    public async Task<ApiSecretDto> CreateApiSecretForClient(string clientId, ApiSecretDto apiSecretDto)
    {
        ArgumentValidation.ValidateString(nameof(clientId), clientId);
        
        var request = new RestRequest("apiSecrets/client/{clientId}", Method.Post);
        request.AddUrlSegment("clientId", clientId);
        request.AddJsonBody(apiSecretDto);

        var response = await Client.ExecutePostAsync<ApiSecretDto>(request);
        ValidateResponse(response);

        return response.Data!;
    }

    public async Task<ApiSecretDto> CreateApiSecretForApiResource(string apiResourceName, ApiSecretDto apiSecretDto)
    {
        ArgumentValidation.ValidateString(nameof(apiResourceName), apiResourceName);

        var request = new RestRequest("apiSecrets/apiResource/{apiResourceName}", Method.Post);
        request.AddUrlSegment("apiResourceName", apiResourceName);
        request.AddJsonBody(apiSecretDto);

        var response = await Client.ExecutePostAsync<ApiSecretDto>(request);
        ValidateResponse(response);

        return response.Data!;
    }

    public async Task UpdateApiSecretClient(string clientId, ApiSecretDto apiSecretDto)
    {
        ArgumentValidation.ValidateString(nameof(clientId), clientId);

        var request = new RestRequest("apiSecrets/client/{clientId}", Method.Put);
        request.AddUrlSegment("clientId", clientId);
        request.AddJsonBody(apiSecretDto);

        var response = await Client.ExecutePostAsync(request);
        ValidateResponse(response);
    }

    public async Task UpdateApiSecretApiResource(string apiResourceName, ApiSecretDto apiSecretDto)
    {
        ArgumentValidation.ValidateString(nameof(apiResourceName), apiResourceName);

        var request = new RestRequest("apiSecrets/apiResource/{apiResourceName}", Method.Put);
        request.AddUrlSegment("apiResourceName", apiResourceName);
        request.AddJsonBody(apiSecretDto);

        var response = await Client.ExecutePostAsync(request);
        ValidateResponse(response);
    }
    
    public async Task<List<ApiResourceDto>> GetApiResources()
    {
        var request = new RestRequest("apiResources");
        var response = await Client.ExecuteAsync<List<ApiResourceDto>>(request);

        ValidateResponse(response);

        return response.Data ?? new List<ApiResourceDto>();
    }

    public async Task CreateApiResource(ApiResourceDto apiResourceDto)
    {
        var request = new RestRequest("apiResources", Method.Post);
        request.AddJsonBody(apiResourceDto);

        var response = await Client.ExecutePostAsync(request);

        ValidateResponse(response);
    }

    public async Task DeleteApiResource(string name)
    {
        ArgumentValidation.ValidateString(nameof(name), name);
        
        var request = new RestRequest("apiResources/{name}", Method.Delete);
        request.AddUrlSegment("name", name);
        
        var response = await Client.ExecuteAsync(request);
        
        ValidateResponse(response);
    }

    public async Task UpdateApiResource(string resourceName, ApiResourceDto apiResourceDto)
    {
        ArgumentValidation.ValidateString(nameof(resourceName), resourceName);

        var request = new RestRequest("apiResources/{name}", Method.Put);
        request.AddUrlSegment("name", resourceName);
        request.AddJsonBody(apiResourceDto);

        var response = await Client.ExecuteAsync(request);

        ValidateResponse(response);
    }

    protected override Uri BuildServiceUri()
    {
        if (string.IsNullOrWhiteSpace(Options.EndpointUri))
        {
            throw new ServiceConfigurationMissingException("Identity services URI is missing.");
        }

        return new Uri(Options.EndpointUri).Append("system").Append("v1");
    }
}
