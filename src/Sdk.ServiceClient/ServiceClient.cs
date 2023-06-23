using System;
using System.Net;
using RestSharp;

namespace Meshmakers.Octo.Sdk.ServiceClient;

public abstract class ServiceClient : IServiceClient
{
    private RestClient? _client;

    protected ServiceClient(ServiceClientOptions options, IServiceClientAccessToken accessToken)
        : this(options)
    {
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
                _client = CreateClient();
            }

            return _client;
        }
    }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    // ReSharper disable once MemberCanBePrivate.Global
    public ServiceClientOptions Options { get; }
    public IServiceClientAccessToken? AccessToken { get; }

    public Uri? ServiceUri { get; private set; }

    private RestClient CreateClient()
    {
        ServiceUri = BuildServiceUri();
        var client = new RestClient(ServiceUri);

        if (AccessToken != null)
        {
            AccessToken.AccessTokenUpdated += (_, _) =>
                UpdateAccessToken(AccessToken.AccessToken);
            UpdateAccessToken(AccessToken.AccessToken);
        }

        return client;
    }

    private void UpdateAccessToken(string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            Client.AddDefaultParameter("Authorization", $"bearer {accessToken}",
                ParameterType.HttpHeader);
        }
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

            throw new ServiceClientResultException(response.Content ?? $"The call was not successful: ${response.StatusCode}",
                response.StatusCode);
        }
    }
}
