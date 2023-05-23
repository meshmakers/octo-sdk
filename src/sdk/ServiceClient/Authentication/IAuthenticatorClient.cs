using System.Threading.Tasks;
using Meshmakers.Octo.Common.Shared;
using Meshmakers.Octo.Common.Shared.Authorization;

namespace Meshmakers.Octo.Sdk.Client.Authentication;

public interface IAuthenticatorClient : IAuthorizationClient
{
    Task<EnsureAuthenticatedData> EnsureAuthenticatedAsync(string refreshToken, string accessToken);

    Task<DeviceAuthenticationRequestData> RequestDeviceAuthorizationAsync(CommonConstants.ApiScopes apiScopes);

    Task<DeviceAuthenticationData> RequestDeviceTokenAsync(string deviceCode);

    Task<AuthenticationData> RequestClientCredentialsTokenAsync(CommonConstants.ApiScopes apiScopes,
        CommonConstants.DefaultScopes defaultScopes);

    Task<AuthenticationData> RequestPasswordTokenAsync(string username, string password,
        CommonConstants.ApiScopes apiScopes);

    Task<AuthenticationData> RefreshTokenAsync(string refreshToken);
}
