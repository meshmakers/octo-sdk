using System;
using Meshmakers.Octo.Sdk.Client.AssetRepositoryServices.System;
using Meshmakers.Octo.Sdk.Client.BotServices;
using Meshmakers.Octo.Sdk.Client.IdentityServices;
using Meshmakers.Octo.Sdk.Client.PlugControllerServices;

namespace Meshmakers.Octo.Sdk.Client.AssetRepositoryServices.Tenants;

// ReSharper disable once UnusedType.Global
public class ServiceClientAccessToken : ITenantClientAccessToken, IBotServiceClientAccessToken,
    IIdentityServiceClientAccessToken, IAssetServiceClientAccessToken, IPlugControllerServiceClientAccessToken
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
