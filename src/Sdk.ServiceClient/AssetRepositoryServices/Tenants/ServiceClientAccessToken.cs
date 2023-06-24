using System;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.System;
using Meshmakers.Octo.Sdk.ServiceClient.BotServices;
using Meshmakers.Octo.Sdk.ServiceClient.IdentityServices;

namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;

// ReSharper disable once UnusedType.Global
public class ServiceClientAccessToken : ITenantClientAccessToken, IBotServiceClientAccessToken,
    IIdentityServiceClientAccessToken, IAssetServiceClientAccessToken
{
    private string? _accessToken;

    public event EventHandler? AccessTokenUpdated;

    public string? AccessToken
    {
        get => _accessToken;
        set
        {
            if (_accessToken != value)
            {
                _accessToken = value;
                OnAccessTokenUpdated();
            }
        }
    }

    protected virtual void OnAccessTokenUpdated()
    {
        AccessTokenUpdated?.Invoke(this, EventArgs.Empty);
    }
}
