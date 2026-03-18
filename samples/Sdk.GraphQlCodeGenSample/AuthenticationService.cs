using System.IdentityModel.Tokens.Jwt;
using Meshmakers.Octo.Communication.Contracts;
using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.Authentication;
using NLog;

namespace Sdk.GraphQlCodeGenSample;

internal class AuthenticationService(IAuthenticatorClient authenticatorClient) : IAuthenticationService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private string? _accessToken;

    public async Task EnsureAuthenticated(IServiceClientAccessToken serviceClientAccessToken)
    {
        Logger.Debug("Ensuring authentication");
        
        if (string.IsNullOrEmpty(_accessToken))
        {
            Logger.Debug("No credential data available.");
            await GetNewAccessToken(serviceClientAccessToken);
            return;
        }

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(_accessToken);

        if (token.ValidTo < DateTime.UtcNow)
        {
            Logger.Debug("Token expired. Refreshing.");
            await GetNewAccessToken(serviceClientAccessToken);
        }
        else
        {
            serviceClientAccessToken.AccessToken = _accessToken;
        }
    }

    private async Task GetNewAccessToken(IServiceClientAccessToken serviceClientAccessToken)
    {
        Logger.Debug("Get new Access Token.");
        var result = await authenticatorClient.RequestClientCredentialsTokenAsync(
            ApiScopes.OctoApiFullAccess,
            DefaultScopes.None);

        _accessToken = result.AccessToken;
        serviceClientAccessToken.AccessToken = result.AccessToken;
        Logger.Debug("New Access Token received.");
    }
}