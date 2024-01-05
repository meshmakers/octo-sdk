using IdentityModel;
using IdentityModel.Client;
using Meshmakers.Common.Shared;
using Meshmakers.Octo.Communication.Contracts;
using Meshmakers.Octo.Sdk.ServiceClient.Authorization;
using Microsoft.Extensions.Options;

namespace Meshmakers.Octo.Sdk.ServiceClient.Authentication;

/// <summary>
///     Implementation of the client proxy for authentication.
/// </summary>
public class AuthenticatorClient : AuthorizationClient, IAuthenticatorClient
{
    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="options">Options for configuration of the client proxy.</param>
    public AuthenticatorClient(IOptionsMonitor<AuthenticatorOptions> options)
        : base(options)
    {
    }

    /// <inheritdoc />
    public async Task<EnsureAuthenticatedData> EnsureAuthenticatedAsync(string refreshToken, string accessToken)
    {
        ArgumentValidation.ValidateString(nameof(refreshToken), refreshToken);
        ArgumentValidation.ValidateString(nameof(accessToken), accessToken);

        var userInfoData = await GetUserInfoAsync(accessToken);
        if (!userInfoData.IsAuthenticated)
        {
            var authenticationData = await RefreshTokenAsync(refreshToken);
            return new EnsureAuthenticatedData
                { IsRefreshDone = true, RefreshedAuthenticationData = authenticationData };
        }

        return new EnsureAuthenticatedData { IsRefreshDone = false };
    }

    /// <inheritdoc />
    public async Task<AuthenticationData> RequestClientCredentialsTokenAsync(CommonConstants.ApiScopes apiScopes,
        CommonConstants.DefaultScopes defaultScopes, IEnumerable<string>? customScopes = null)
    {
        var disco = await GetDiscoveryResponse();

        var client = new HttpClient();
        var response = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            GrantType = OidcConstants.GrantTypes.ClientCredentials,

            Address = disco.TokenEndpoint,

            ClientId = Options.ClientId,
            ClientSecret = Options.ClientSecret,

            Scope = CommonConstants.GetScopes(apiScopes, customScopes, defaultScopes)
        });

        ValidateResponse(response);

        return new AuthenticationData
        {
            AccessToken = response.AccessToken,
            RefreshToken = response.RefreshToken,
            ExpiresAt = DateTime.Now.AddSeconds(response.ExpiresIn)
        };
    }

    /// <inheritdoc />
    public async Task<DeviceAuthenticationRequestData> RequestDeviceAuthorizationAsync(
        CommonConstants.ApiScopes apiScopes, IEnumerable<string>? customScopes = null)
    {
        var disco = await GetDiscoveryResponse();

        var client = new HttpClient();
        var response = await client.RequestDeviceAuthorizationAsync(new DeviceAuthorizationRequest
        {
            Address = disco.DeviceAuthorizationEndpoint,

            ClientId = Options.ClientId,
            ClientSecret = Options.ClientSecret,

            Scope = CommonConstants.GetScopes(apiScopes, customScopes,
                CommonConstants.DefaultScopes.UserDefault | CommonConstants.DefaultScopes.OfflineAccess)
        });

        ValidateResponse(response);

        return new DeviceAuthenticationRequestData
        {
            UserCode = response.UserCode,
            DeviceCode = response.DeviceCode,
            VerificationUri = response.VerificationUri,
            VerificationUriComplete = response.VerificationUriComplete,
            PollingInterval = response.Interval,
            ExpiresAt = response.ExpiresIn.HasValue
                ? DateTime.Now.AddSeconds(response.ExpiresIn.Value)
                : default(DateTime?)
        };
    }

    /// <inheritdoc />
    public async Task<DeviceAuthenticationData> RequestDeviceTokenAsync(string deviceCode)
    {
        ArgumentValidation.ValidateString(nameof(deviceCode), deviceCode);

        var disco = await GetDiscoveryResponse();

        var client = new HttpClient();
        var response = await client.RequestDeviceTokenAsync(new DeviceTokenRequest
        {
            Address = disco.TokenEndpoint,

            ClientId = Options.ClientId,
            ClientSecret = Options.ClientSecret,

            DeviceCode = deviceCode
        });

        if (response.IsError && (response.Error == OidcConstants.TokenErrors.AuthorizationPending ||
                                 response.Error == OidcConstants.TokenErrors.SlowDown))
        {
            return new DeviceAuthenticationData { IsAuthenticationPending = true };
        }

        ValidateResponse(response);

        return new DeviceAuthenticationData
        {
            IsAuthenticationPending = false,
            AccessToken = response.AccessToken,
            RefreshToken = response.RefreshToken,
            ExpiresAt = DateTime.Now.AddSeconds(response.ExpiresIn)
        };
    }

    /// <inheritdoc />
    public async Task<AuthenticationData> RequestPasswordTokenAsync(string username, string password,
        CommonConstants.ApiScopes apiScopes, IEnumerable<string>? customScopes = null)
    {
        ArgumentValidation.ValidateString(nameof(username), username);
        ArgumentValidation.ValidateString(nameof(password), password);

        var disco = await GetDiscoveryResponse();

        var client = new HttpClient();
        var response = await client.RequestPasswordTokenAsync(new PasswordTokenRequest
        {
            Address = disco.TokenEndpoint,

            ClientId = Options.ClientId,
            ClientSecret = Options.ClientSecret,

            UserName = username,
            Password = password,

            Scope = CommonConstants.GetScopes(apiScopes, customScopes,
                CommonConstants.DefaultScopes.UserDefault | CommonConstants.DefaultScopes.OfflineAccess)
        });

        ValidateResponse(response);

        return new AuthenticationData
        {
            AccessToken = response.AccessToken,
            RefreshToken = response.RefreshToken,
            ExpiresAt = DateTime.Now.AddSeconds(response.ExpiresIn)
        };
    }

    /// <inheritdoc />
    public async Task<AuthenticationData> RefreshTokenAsync(string refreshToken)
    {
        ArgumentValidation.ValidateString(nameof(refreshToken), refreshToken);

        var disco = await GetDiscoveryResponse();

        var client = new HttpClient();
        var response = await client.RequestRefreshTokenAsync(new RefreshTokenRequest
        {
            Address = disco.TokenEndpoint,

            ClientId = Options.ClientId,
            ClientSecret = Options.ClientSecret,

            RefreshToken = refreshToken
        });
        ValidateResponse(response);

        return new AuthenticationData
        {
            AccessToken = response.AccessToken,
            RefreshToken = response.RefreshToken,
            ExpiresAt = DateTime.Now.AddSeconds(response.ExpiresIn)
        };
    }

    private static void ValidateResponse(ProtocolResponse response)
    {
        if (response.IsError)
        {
            throw AuthenticationFailedException.RequestFailed(response.Error, response.Exception);
        }
    }
}