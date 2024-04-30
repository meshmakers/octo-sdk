using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.StreamData;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.System;
using Meshmakers.Octo.Sdk.ServiceClient.BotServices;
using Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;
using Meshmakers.Octo.Sdk.ServiceClient.IdentityServices;

namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;

/// <summary>
///     Implements the access token for the service client.
/// </summary>
// ReSharper disable once UnusedType.Global
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class ServiceClientAccessToken : ITenantClientAccessToken, IBotServiceClientAccessToken,
    IIdentityServiceClientAccessToken, IAssetServiceClientAccessToken, ICommunicationServiceClientAccessToken, IStreamDataServiceClientAccessToken
{
    private string? _accessToken;

    /// <inheritdoc />
    public event EventHandler? AccessTokenUpdated;

    /// <inheritdoc />
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

    /// <summary>
    ///     Raises the <see cref="AccessTokenUpdated" /> event.
    /// </summary>
    protected virtual void OnAccessTokenUpdated()
    {
        AccessTokenUpdated?.Invoke(this, EventArgs.Empty);
    }
}