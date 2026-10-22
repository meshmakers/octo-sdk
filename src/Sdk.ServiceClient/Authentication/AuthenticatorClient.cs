using Duende.IdentityModel;
using Duende.IdentityModel.Client;
using Meshmakers.Common.Shared;
using Meshmakers.Octo.Communication.Contracts;
using Meshmakers.Octo.Sdk.ServiceClient.Authorization;
using Microsoft.Extensions.Options;

namespace Meshmakers.Octo.Sdk.ServiceClient.Authentication;

/// <summary>
///     Implementation of the client proxy for authentication.
/// </summary>
public class AuthenticatorClient : AuthorizationClient, IAuthenticatorClient, ITokenExchangeClient
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
    public Task<AuthenticationData> RequestClientCredentialsTokenAsync(ApiScopes apiScopes,
        DefaultScopes defaultScopes, IEnumerable<string>? customScopes = null,
        string? clientId = null, string? clientSecret = null)
    {
        return RequestClientCredentialsTokenCoreAsync(apiScopes, defaultScopes, customScopes, clientId, clientSecret,
            Options.TenantId);
    }

    /// <inheritdoc />
    public Task<AuthenticationData> RequestClientCredentialsTokenForTenantAsync(string tenantId, ApiScopes apiScopes,
        DefaultScopes defaultScopes, string clientId, string clientSecret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientSecret);

        return RequestClientCredentialsTokenCoreAsync(apiScopes, defaultScopes, null, clientId, clientSecret, tenantId);
    }

    private async Task<AuthenticationData> RequestClientCredentialsTokenCoreAsync(ApiScopes apiScopes,
        DefaultScopes defaultScopes, IEnumerable<string>? customScopes, string? clientId, string? clientSecret,
        string? tenantId)
    {
        var disco = await GetDiscoveryResponse();

        var request = BuildClientCredentialsTokenRequest(
            tokenEndpoint: Rebase(disco.TokenEndpoint),
            clientId: clientId ?? Options.ClientId,
            clientSecret: clientSecret ?? Options.ClientSecret,
            scope: CommonConstants.GetScopes(apiScopes, customScopes, defaultScopes),
            tenantId: tenantId);

        using var client = ServerCertificateTrust.CreateHttpClient();
        var response = await client.RequestClientCredentialsTokenAsync(request);

        ValidateResponse(response);

        return new AuthenticationData
        {
            AccessToken = response.AccessToken,
            RefreshToken = response.RefreshToken,
            ExpiresAt = DateTime.Now.AddSeconds(response.ExpiresIn)
        };
    }

    /// <summary>
    ///     Builds the client-credentials token request. Extracted for unit testing of the
    ///     <c>acr_values</c> injection and parameter mapping; the actual HTTP call is the
    ///     small wrapper above.
    /// </summary>
    internal static ClientCredentialsTokenRequest BuildClientCredentialsTokenRequest(
        string? tokenEndpoint, string clientId, string? clientSecret, string scope, string? tenantId)
    {
        var request = new ClientCredentialsTokenRequest
        {
            GrantType = OidcConstants.GrantTypes.ClientCredentials,
            Address = tokenEndpoint,
            ClientId = clientId,
            ClientSecret = clientSecret,
            Scope = scope
        };

        if (!string.IsNullOrEmpty(tenantId))
        {
            request.Parameters.Add(OidcConstants.AuthorizeRequest.AcrValues, $"tenant:{tenantId}");
        }

        return request;
    }

    /// <inheritdoc />
    public async Task<DeviceAuthenticationRequestData> RequestDeviceAuthorizationAsync(
        ApiScopes apiScopes, IEnumerable<string>? customScopes = null)
    {
        var disco = await GetDiscoveryResponse();

        using var client = ServerCertificateTrust.CreateHttpClient();
        var request = new DeviceAuthorizationRequest
        {
            Address = Rebase(disco.DeviceAuthorizationEndpoint),

            ClientId = Options.ClientId,
            ClientSecret = Options.ClientSecret,

            Scope = CommonConstants.GetScopes(apiScopes, customScopes,
                DefaultScopes.UserDefault | DefaultScopes.OfflineAccess)
        };

        if (!string.IsNullOrEmpty(Options.TenantId))
        {
            request.Parameters.Add(OidcConstants.AuthorizeRequest.AcrValues, $"tenant:{Options.TenantId}");
        }

        var response = await client.RequestDeviceAuthorizationAsync(request);

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

        using var client = ServerCertificateTrust.CreateHttpClient();
        var response = await client.RequestDeviceTokenAsync(new DeviceTokenRequest
        {
            Address = Rebase(disco.TokenEndpoint),

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
        ApiScopes apiScopes, IEnumerable<string>? customScopes = null)
    {
        ArgumentValidation.ValidateString(nameof(username), username);
        ArgumentValidation.ValidateString(nameof(password), password);

        var disco = await GetDiscoveryResponse();

        using var client = ServerCertificateTrust.CreateHttpClient();
        var response = await client.RequestPasswordTokenAsync(new PasswordTokenRequest
        {
            Address = Rebase(disco.TokenEndpoint),

            ClientId = Options.ClientId,
            ClientSecret = Options.ClientSecret,

            UserName = username,
            Password = password,

            Scope = CommonConstants.GetScopes(apiScopes, customScopes,
                DefaultScopes.UserDefault | DefaultScopes.OfflineAccess)
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

        using var client = ServerCertificateTrust.CreateHttpClient();
        var request = new RefreshTokenRequest
        {
            Address = Rebase(disco.TokenEndpoint),

            ClientId = Options.ClientId,
            ClientSecret = Options.ClientSecret,

            RefreshToken = refreshToken
        };

        if (!string.IsNullOrEmpty(Options.TenantId))
        {
            request.Parameters.Add(OidcConstants.AuthorizeRequest.AcrValues, $"tenant:{Options.TenantId}");
        }

        var response = await client.RequestRefreshTokenAsync(request);
        ValidateResponse(response);

        return new AuthenticationData
        {
            AccessToken = response.AccessToken,
            RefreshToken = response.RefreshToken,
            ExpiresAt = DateTime.Now.AddSeconds(response.ExpiresIn)
        };
    }

    /// <inheritdoc />
    public async Task<AuthenticationData> ExchangeTokenForTenantAsync(string subjectToken, string targetTenantId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetTenantId))
        {
            throw new ArgumentException("The target tenant is required.", nameof(targetTenantId));
        }

        if (string.IsNullOrWhiteSpace(subjectToken))
        {
            throw new TenantTokenExchangeDeniedException(targetTenantId, TenantTokenDenialCause.SubjectTokenRejected);
        }

        TokenResponse response;
        try
        {
            var disco = await GetDiscoveryResponse();
            var request = BuildTokenExchangeRequest(Rebase(disco.TokenEndpoint), Options.ClientId,
                Options.ClientSecret, subjectToken, targetTenantId);
            using var client = ServerCertificateTrust.CreateHttpClient();
            response = await client.RequestTokenExchangeTokenAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException
                                       or ServiceConfigurationMissingException)
        {
            throw new TenantTokenExchangeDeniedException(targetTenantId, TenantTokenDenialCause.IdentityUnreachable,
                inner: ex);
        }

        if (response.IsError)
        {
            if (response.ErrorType == ResponseErrorType.Http || response.ErrorType == ResponseErrorType.Exception)
            {
                throw new TenantTokenExchangeDeniedException(targetTenantId,
                    TenantTokenDenialCause.IdentityUnreachable, response.Error, response.Exception);
            }

            throw new TenantTokenExchangeDeniedException(targetTenantId,
                MapExchangeError(response.Error, response.ErrorDescription), response.Error);
        }

        if (string.IsNullOrWhiteSpace(response.AccessToken))
        {
            throw new TenantTokenExchangeDeniedException(targetTenantId, TenantTokenDenialCause.InvalidResponse);
        }

        return new AuthenticationData
        {
            AccessToken = response.AccessToken,
            // Exchanged tokens carry no refresh token: re-exchange from the token it was issued from.
            RefreshToken = null,
            ExpiresAt = DateTime.Now.AddSeconds(response.ExpiresIn)
        };
    }

    /// <summary>
    ///     Builds the RFC 8693 token-exchange request. Extracted for unit testing.
    /// </summary>
    internal static TokenExchangeTokenRequest BuildTokenExchangeRequest(string? tokenEndpoint, string clientId,
        string? clientSecret, string subjectToken, string targetTenantId)
    {
        var request = new TokenExchangeTokenRequest
        {
            Address = tokenEndpoint,
            ClientId = clientId,
            ClientSecret = clientSecret,
            SubjectToken = subjectToken,
            SubjectTokenType = OidcConstants.TokenTypeIdentifiers.AccessToken,
            Scope = CommonConstants.GetScopes(ApiScopes.OctoApiFullAccess, null, DefaultScopes.UserDefault)
        };
        request.Parameters.Add(OidcConstants.AuthorizeRequest.AcrValues, $"tenant:{targetTenantId}");
        return request;
    }

    /// <summary>
    ///     Maps an OAuth error of the identity service to the cause the user is told about. The error
    ///     descriptions are those of the identity service's tenant exchange; an explicit
    ///     <c>tenant_not_provisioned</c> error (AB#6839) is mapped as soon as identity sends it.
    /// </summary>
    internal static TenantTokenDenialCause MapExchangeError(string? error, string? description)
    {
        var text = description ?? string.Empty;
        return error switch
        {
            "tenant_not_provisioned" => TenantTokenDenialCause.NotProvisioned,
            "unsupported_grant_type" => TenantTokenDenialCause.ClientNotAllowed,
            "invalid_client" => TenantTokenDenialCause.ClientNotAllowed,
            "unauthorized_client" when text.Contains("cross-tenant", StringComparison.OrdinalIgnoreCase) =>
                TenantTokenDenialCause.AccessToTenantDenied,
            "unauthorized_client" => TenantTokenDenialCause.ClientNotAllowed,
            "access_denied" => TenantTokenDenialCause.AccessToTenantDenied,
            "invalid_target" => TenantTokenDenialCause.TenantUnknown,
            "invalid_grant" when text.Contains("subject_token", StringComparison.OrdinalIgnoreCase) =>
                TenantTokenDenialCause.SubjectTokenRejected,
            "invalid_grant" => TenantTokenDenialCause.AccessToTenantDenied,
            _ => TenantTokenDenialCause.Unknown
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