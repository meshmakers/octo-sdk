using IdentityModel.Client;
using Meshmakers.Common.Shared;
using Microsoft.Extensions.Options;

// ReSharper disable UnusedType.Global

namespace Meshmakers.Octo.Sdk.ServiceClient.Authorization;

/// <summary>
///     Implements <see cref="IAuthorizationClient" /> using IdentityModel.
/// </summary>
public class AuthorizationClient : IAuthorizationClient
{
    private IDiscoveryCache? _cache;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AuthorizationClient" /> class.
    /// </summary>
    /// <param name="options"></param>
    public AuthorizationClient(IOptionsMonitor<AuthorizationOptions> options)
    {
        Options = options.CurrentValue;

        options.OnChange(CreateCache);
        if (!string.IsNullOrWhiteSpace(options.CurrentValue.IssuerUri))
        {
            CreateCache(options.CurrentValue);
        }
    }

    private IDiscoveryCache Cache
    {
        get
        {
            if (_cache == null)
            {
                throw new ServiceConfigurationMissingException("Discovery cache not initialized.");
            }

            return _cache;
        }
    }

    // ReSharper disable once MemberCanBePrivate.Global
    /// <summary>
    ///     Gets the options used to configure the client.
    /// </summary>
    protected AuthorizationOptions Options { get; private set; }

    /// <inheritdoc />
    public async Task<UserInfoData> GetUserInfoAsync(string accessToken)
    {
        ArgumentValidation.ValidateString(nameof(accessToken), accessToken);

        var disco = await GetDiscoveryResponse();

        var client = new HttpClient();

        var response = await client.GetUserInfoAsync(new UserInfoRequest
        {
            Address = disco.UserInfoEndpoint,
            Token = accessToken
        });

        return response.IsError ? new UserInfoData(false, null) : new UserInfoData(true, response.Claims);
    }

    /// <inheritdoc />
    public async Task<bool> IntrospectApiResource(string accessToken, string apiName, string apiSecret)
    {
        ArgumentValidation.ValidateString(nameof(accessToken), accessToken);
        ArgumentValidation.ValidateString(nameof(apiName), apiName);
        ArgumentValidation.ValidateString(nameof(apiSecret), apiSecret);

        var disco = await GetDiscoveryResponse();

        var client = new HttpClient();
        var result = await client.IntrospectTokenAsync(new TokenIntrospectionRequest
        {
            Address = disco.IntrospectionEndpoint,

            ClientId = apiName,
            ClientSecret = apiSecret,

            Token = accessToken
        });

        return !result.IsError && result.IsActive;
    }

    private void CreateCache(AuthorizationOptions authorizationOptions)
    {
        Options = authorizationOptions;

        if (string.IsNullOrWhiteSpace(Options.IssuerUri))
        {
            throw new ServiceConfigurationMissingException("Issuer URI is not configured.");
        }

        var url = new Uri(Options.IssuerUri);
        _cache = new DiscoveryCache(url.AbsoluteUri.TrimEnd('/'));
    }

    private static void ValidateResponse(ProtocolResponse response)
    {
        if (response.IsError)
        {
            throw AuthorizationFailedException.AuthenticationFailed(response.Error, response.Exception);
        }
    }

    // ReSharper disable once MemberCanBePrivate.Global
    /// <summary>
    ///     Gets the discovery response.
    /// </summary>
    /// <returns></returns>
    protected async Task<DiscoveryDocumentResponse> GetDiscoveryResponse()
    {
        var disco = await Cache.GetAsync();
        ValidateResponse(disco);

        return disco;
    }
}