using Meshmakers.Common.Shared;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Meshmakers.Octo.Sdk.ServiceClient.IdentityServices;

/// <summary>
///     Implementation of the identity services client.
/// </summary>
public class IdentityServicesClient : ServiceClient, IIdentityServicesClient
{
    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="serviceClientOptions">Options for configuration of the client proxy.</param>
    /// <param name="identityAccessToken">The access token management object</param>
    public IdentityServicesClient(IOptions<IdentityServiceClientOptions> serviceClientOptions,
        IIdentityServiceClientAccessToken identityAccessToken)
        : this(serviceClientOptions.Value, identityAccessToken)
    {
    }

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="serviceClientOptions">Options for configuration of the client proxy.</param>
    /// <param name="identityAccessToken">The access token management object</param>
    public IdentityServicesClient(IdentityServiceClientOptions serviceClientOptions,
        IIdentityServiceClientAccessToken identityAccessToken)
        : base(serviceClientOptions, identityAccessToken)
    {
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IdentityProviderDto>> GetIdentityProviders()
    {
        var request = new RestRequest("identityProviders");

        var response = await Client.ExecuteAsync<IdentityProvidersResult>(request);
        ValidateResponse(response);

        return response.Data?.IdentityProviders ?? new List<IdentityProviderDto>();
    }

    /// <inheritdoc />
    public async Task<IdentityProviderDto> GetIdentityProvider(OctoObjectId rtId)
    {
        var request = new RestRequest("identityProviders/{rtId}");
        request.AddUrlSegment("rtId", rtId);

        var response = await Client.ExecuteAsync<IdentityProviderDto>(request);
        ValidateResponse(response);

        return response.Data!;
    }

    /// <inheritdoc />
    public async Task CreateIdentityProvider(IdentityProviderDto identityProvider)
    {
        var request = new RestRequest("identityProviders", Method.Post);
        request.AddJsonBody(identityProvider);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task UpdateIdentityProvider(OctoObjectId rtId, IdentityProviderDto identityProvider)
    {
        var request = new RestRequest("identityProviders/{rtId}", Method.Put);
        request.AddUrlSegment("rtId", rtId);
        request.AddJsonBody(identityProvider);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task DeleteIdentityProvider(OctoObjectId rtId)
    {
        var request = new RestRequest("identityProviders/{rtId}", Method.Delete);
        request.AddUrlSegment("rtId", rtId);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }


    /// <inheritdoc />
    public async Task<IEnumerable<ClientDto>> GetClients()
    {
        var request = new RestRequest("clients");

        var response = await Client.ExecuteAsync<List<ClientDto>>(request);
        ValidateResponse(response);

        return response.Data ?? new List<ClientDto>();
    }

    /// <inheritdoc />
    public async Task<ClientDto> GetClient(string clientId)
    {
        ArgumentValidation.ValidateString(nameof(clientId), clientId);

        var request = new RestRequest("clients/{id}");
        request.AddUrlSegment("id", clientId);

        var response = await Client.ExecuteAsync<ClientDto>(request);
        ValidateResponse(response);

        return response.Data!;
    }

    /// <inheritdoc />
    public async Task CreateClient(ClientDto client)
    {
        var request = new RestRequest("clients", Method.Post);
        request.AddJsonBody(client);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task UpdateClient(string clientId, ClientDto client)
    {
        ArgumentValidation.ValidateString(nameof(clientId), clientId);

        var request = new RestRequest("clients/{id}", Method.Put);
        request.AddUrlSegment("id", clientId);
        request.AddJsonBody(client);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task DeleteClient(string clientId)
    {
        ArgumentValidation.ValidateString(nameof(clientId), clientId);

        var request = new RestRequest("clients/{id}", Method.Delete);
        request.AddUrlSegment("id", clientId);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<UserDto>> GetUsers()
    {
        var request = new RestRequest("users");

        var response = await Client.ExecuteAsync<List<UserDto>>(request);
        ValidateResponse(response);

        return response.Data ?? new List<UserDto>();
    }

    /// <inheritdoc />
    public async Task<UserDto> GetUserByNameEmailOrId(string userNameOrEMailAddress)
    {
        var request = new RestRequest($"users/{userNameOrEMailAddress}");

        var response = await Client.ExecuteAsync<UserDto>(request);
        ValidateResponse(response);

        return response.Data!;
    }

    /// <inheritdoc />
    public async Task AddRoleToUser(string userNameOrEMailAddress, string roleId)
    {
        var request = new RestRequest($"users/{userNameOrEMailAddress}/roles/{roleId}", Method.Put);

        var response = await Client.ExecuteAsync(request);

        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task RemoveRoleFromUser(string userNameOrEMailAddress, string roleId)
    {
        var request = new RestRequest($"users/{userNameOrEMailAddress}/roles/{roleId}", Method.Delete);

        var response = await Client.ExecuteAsync(request);

        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task CreateUser(UserDto userDto)
    {
        var request = new RestRequest("users", Method.Post);
        request.AddJsonBody(userDto);

        var response = await Client.ExecutePostAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task UpdateUser(string userName, UserDto userDto)
    {
        ArgumentValidation.ValidateString(nameof(userName), userName);

        var request = new RestRequest("users/{userName}", Method.Put);
        request.AddUrlSegment("userName", userName);
        request.AddJsonBody(userDto);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task DeleteUser(string userName)
    {
        ArgumentValidation.ValidateString(nameof(userName), userName);

        var request = new RestRequest("users/{userName}", Method.Delete);
        request.AddUrlSegment("userName", userName);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task ResetPassword(string userName, string password)
    {
        ArgumentValidation.ValidateString(nameof(userName), userName);
        ArgumentValidation.ValidateString(nameof(password), password);

        var request = new RestRequest("users/resetPassword", Method.Post);
        request.AddQueryParameter("userName", userName);
        request.AddQueryParameter("password", password);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task<RoleDto> GetRoleByName(string roleName)
    {
        ArgumentValidation.ValidateString(nameof(roleName), roleName);

        var request = new RestRequest("roles/names/{roleName}");
        request.AddUrlSegment("roleName", roleName);

        var response = await Client.ExecuteAsync<RoleDto>(request);
        ValidateResponse(response);

        return response.Data!;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RoleDto>> GetRoles()
    {
        var request = new RestRequest("roles");

        var response = await Client.ExecuteAsync<List<RoleDto>>(request);
        ValidateResponse(response);

        return response.Data ?? new List<RoleDto>();
    }

    /// <inheritdoc />
    public async Task CreateRole(RoleDto roleDto)
    {
        var request = new RestRequest("roles", Method.Post);
        request.AddJsonBody(roleDto);

        var response = await Client.ExecutePostAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task UpdateRole(string roleName, RoleDto roleDto)
    {
        ArgumentValidation.ValidateString(nameof(roleName), roleName);

        var request = new RestRequest("roles/{roleName}", Method.Put);
        request.AddUrlSegment("roleName", roleName);
        request.AddJsonBody(roleDto);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task DeleteRole(string roleName)
    {
        ArgumentValidation.ValidateString(nameof(roleName), roleName);

        var request = new RestRequest("roles/{roleName}", Method.Delete);
        request.AddUrlSegment("roleName", roleName);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ApiScopeDto>> GetApiScopes()
    {
        var request = new RestRequest("apiScopes");

        var response = await Client.ExecuteAsync<List<ApiScopeDto>>(request);
        ValidateResponse(response);

        return response.Data ?? new List<ApiScopeDto>();
    }

    /// <inheritdoc />
    public async Task<ApiScopeDto> GetApiScope(string name)
    {
        ArgumentValidation.ValidateString(nameof(name), name);

        var request = new RestRequest("apiScopes/{name}");
        request.AddUrlSegment("name", name);

        var response = await Client.ExecuteAsync<ApiScopeDto>(request);
        ValidateResponse(response);

        return response.Data!;
    }

    /// <inheritdoc />
    public async Task DeleteScope(string name)
    {
        ArgumentValidation.ValidateString(nameof(name), name);

        var request = new RestRequest("apiScopes/{name}", Method.Delete);
        request.AddUrlSegment("name", name);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task UpdateApiScope(string name, ApiScopeDto scopeDto)
    {
        ArgumentValidation.ValidateString(nameof(name), name);

        var request = new RestRequest("apiScopes/{name}", Method.Put);
        request.AddUrlSegment("name", name);
        request.AddJsonBody(scopeDto);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task CreateApiScope(ApiScopeDto apiScopeDto)
    {
        var request = new RestRequest("apiScopes", Method.Post);
        request.AddJsonBody(apiScopeDto);

        var response = await Client.ExecutePostAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<IEnumerable<ApiSecretDto>> GetApiSecretsForClient(string clientId)
    {
        ArgumentValidation.ValidateString(nameof(clientId), clientId);

        var request = new RestRequest("apiSecrets/client/{clientId}");
        request.AddUrlSegment("clientId", clientId);

        var response = await Client.ExecuteAsync<List<ApiSecretDto>>(request);
        ValidateResponse(response);

        return response.Data ?? new List<ApiSecretDto>();
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<IEnumerable<ApiSecretDto>> GetApiSecretsForApiResource(string apiResourceName)
    {
        ArgumentValidation.ValidateString(nameof(apiResourceName), apiResourceName);

        var request = new RestRequest("apiSecrets/apiResource/{apiResourceName}");
        request.AddUrlSegment("apiResourceName", apiResourceName);

        var response = await Client.ExecuteAsync<List<ApiSecretDto>>(request);
        ValidateResponse(response);

        return response.Data ?? new List<ApiSecretDto>();
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task UpdateApiSecretClient(string clientId, ApiSecretDto apiSecretDto)
    {
        ArgumentValidation.ValidateString(nameof(clientId), clientId);

        var request = new RestRequest("apiSecrets/client/{clientId}", Method.Put);
        request.AddUrlSegment("clientId", clientId);
        request.AddJsonBody(apiSecretDto);

        var response = await Client.ExecutePostAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task UpdateApiSecretApiResource(string apiResourceName, ApiSecretDto apiSecretDto)
    {
        ArgumentValidation.ValidateString(nameof(apiResourceName), apiResourceName);

        var request = new RestRequest("apiSecrets/apiResource/{apiResourceName}", Method.Put);
        request.AddUrlSegment("apiResourceName", apiResourceName);
        request.AddJsonBody(apiSecretDto);

        var response = await Client.ExecutePostAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task<List<ApiResourceDto>> GetApiResources()
    {
        var request = new RestRequest("apiResources");
        var response = await Client.ExecuteAsync<List<ApiResourceDto>>(request);

        ValidateResponse(response);

        return response.Data ?? new List<ApiResourceDto>();
    }

    /// <inheritdoc />
    public async Task CreateApiResource(ApiResourceDto apiResourceDto)
    {
        var request = new RestRequest("apiResources", Method.Post);
        request.AddJsonBody(apiResourceDto);

        var response = await Client.ExecutePostAsync(request);

        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task DeleteApiResource(string name)
    {
        ArgumentValidation.ValidateString(nameof(name), name);

        var request = new RestRequest("apiResources/{name}", Method.Delete);
        request.AddUrlSegment("name", name);

        var response = await Client.ExecuteAsync(request);

        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task UpdateApiResource(string name, ApiResourceDto apiResourceDto)
    {
        ArgumentValidation.ValidateString(nameof(name), name);

        var request = new RestRequest("apiResources/{name}", Method.Put);
        request.AddUrlSegment("name", name);
        request.AddJsonBody(apiResourceDto);

        var response = await Client.ExecuteAsync(request);

        ValidateResponse(response);
    }
    
    /// <inheritdoc />
    public async Task ReconfigureLogLevelAsync(string loggerName, LogLevelDto minLogLevel, LogLevelDto maxLogLevel)
    {
        var request = new RestRequest("diagnostics/reconfigureLogLevel", Method.Post);
        request.AddQueryParameter("loggerName", loggerName);
        request.AddQueryParameter("minLogLevel", minLogLevel);
        request.AddQueryParameter("maxLogLevel", maxLogLevel);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    protected override Uri BuildServiceUri()
    {
        if (string.IsNullOrWhiteSpace(Options.EndpointUri))
        {
            throw new ServiceConfigurationMissingException("Identity services URI is missing.");
        }

        return new Uri(Options.EndpointUri).Append("system").Append("v1");
    }
}