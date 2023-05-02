using System;
using Meshmakers.Octo.Frontend.Client.System;

namespace Meshmakers.Octo.Frontend.Client.Tenants;

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
