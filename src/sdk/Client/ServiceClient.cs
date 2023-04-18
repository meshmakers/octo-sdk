using System;
using System.Net;
using RestSharp;

namespace Meshmakers.Octo.Frontend.Client;

public abstract class ServiceClient
{
    private RestClient _client;

    protected ServiceClient(ServiceClientOptions options, IServiceClientAccessToken accessToken)
    {
        Options = options;
        AccessToken = accessToken;
    }

    protected ServiceClient(ServiceClientOptions options)
    {
        Options = options;
    }

    protected RestClient Client
    {
        get
        {
            if (_client == null)
            {
                Initialize();
            }

            return _client;
        }
    }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    // ReSharper disable once MemberCanBePrivate.Global
    public ServiceClientOptions Options { get; }
    public IServiceClientAccessToken AccessToken { get; }

    public Uri ServiceUri { get; private set; }

    public void Initialize()
    {
        ServiceUri = BuildServiceUri();
        _client = new RestClient(ServiceUri);

        if (AccessToken != null)
        {
            AccessToken.AccessTokenUpdated += (sender, args) =>
                UpdateAccessToken(AccessToken.AccessToken);
            UpdateAccessToken(AccessToken.AccessToken);
        }
    }

    private void UpdateAccessToken(string accessToken)
    {
        Client.AddDefaultParameter("Authorization", $"bearer {accessToken}",
            ParameterType.HttpHeader);
    }

    protected abstract Uri BuildServiceUri();

    protected static void ValidateResponse(RestResponse response)
    {
        if (!response.IsSuccessful)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedServiceAccessException(response.ErrorException);
            }

            if (!string.IsNullOrEmpty(response.ErrorMessage))
            {
                throw new ServiceClientException(response.ErrorMessage, response.ErrorException);
            }

            throw new ServiceClientResultException(response.Content, response.StatusCode);
        }
    }
}
