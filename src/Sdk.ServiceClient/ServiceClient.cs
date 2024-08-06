using System.Net;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;
using RestSharp;

namespace Meshmakers.Octo.Sdk.ServiceClient;

/// <summary>
///     Implementation of the base interface of REST based service clients.
/// </summary>
public abstract class ServiceClient : IServiceClient
{
    private RestClient? _client;
    private Uri? _uri;

    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="options">Options for configuration of the client proxy.</param>
    /// <param name="accessToken">The access token management object</param>
    protected ServiceClient(ServiceClientOptions options, IServiceClientAccessToken accessToken)
        : this(options)
    {
        AccessToken = accessToken;
    }

    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="options">Options for configuration of the client proxy.</param>
    protected ServiceClient(ServiceClientOptions options)
    {
        Options = options;
        AccessToken = new ServiceClientAccessToken();
    }

    /// <summary>
    ///     Returns the REST HTTP client.
    /// </summary>
    protected RestClient Client
    {
        get
        {
            if (_client == null)
            {
                _client = CreateClient();
                UpdateAccessToken(AccessToken.AccessToken);
            }

            return _client;
        }
    }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    // ReSharper disable once MemberCanBePrivate.Global
    /// <summary>
    ///     Returns the options for configuration of the client proxy.
    /// </summary>
    public ServiceClientOptions Options { get; }

    /// <inheritdoc />
    public IServiceClientAccessToken AccessToken { get; }

    /// <inheritdoc />
    public Uri ServiceUri
    {
        get
        {
            if (_uri == null)
            {
                _uri = BuildServiceUri();
            }

            return _uri;
        }
    }

    private RestClient CreateClient()
    {
        var client = new RestClient(ServiceUri,
            options => options.Timeout = TimeSpan.FromMilliseconds(Options.MaxTimeout));

        AccessToken.AccessTokenUpdated += (_, _) =>
            UpdateAccessToken(AccessToken.AccessToken);


        return client;
    }

    private void UpdateAccessToken(string? accessToken)
    {
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            Client.AddDefaultParameter("Authorization", $"bearer {accessToken}",
                ParameterType.HttpHeader);
        }
    }

    /// <summary>
    ///     Builds the service URI.
    /// </summary>
    /// <returns></returns>
    protected abstract Uri BuildServiceUri();

    /// <summary>
    ///     Validates the response of a HTTP call.
    /// </summary>
    /// <param name="response">The response object.</param>
    /// <exception cref="UnauthorizedServiceAccessException"></exception>
    /// <exception cref="ServiceClientException"></exception>
    /// <exception cref="ServiceClientResultException"></exception>
    protected static void ValidateResponse(RestResponse response)
    {
        if (!response.IsSuccessful)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedServiceAccessException(response.ErrorException);
            }

            if (!string.IsNullOrEmpty(response.Content))
            {
                throw new ServiceClientResultException(
                    response.Content,
                    response.StatusCode,
                    response.ErrorException);
            }

            if (!string.IsNullOrEmpty(response.ErrorMessage))
            {
                throw new ServiceClientResultException(response.ErrorMessage, response.StatusCode,
                    response.ErrorException);
            }

            throw new ServiceClientResultException($"The call was not successful: ${response.StatusCode}",
                response.StatusCode, response.ErrorException);
        }
    }
}