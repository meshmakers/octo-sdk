using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Meshmakers.Octo.Common.Shared;
using Microsoft.Extensions.Options;

namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;

/// <summary>
/// Implementation tenant specific proxy of the <see cref="ITenantClient"/> interface.
/// </summary>
public class TenantClient : ITenantClient
{
    private GraphQLHttpClient? _client;
    private Uri? _serviceUri;

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="dataSourceClientOptions">Options for data source client using DI</param>
    /// <param name="tenantClientAccessToken">Access Token for backend access</param>
    // ReSharper disable once UnusedMember.Global
    public TenantClient(IOptions<TenantClientOptions> dataSourceClientOptions,
        ITenantClientAccessToken tenantClientAccessToken)
        : this(dataSourceClientOptions.Value, tenantClientAccessToken)
    {
    }

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="tenantClientOptions">Options for data source client</param>
    /// <param name="tenantClientAccessToken">Access Token for backend access</param>
    // ReSharper disable once MemberCanBePrivate.Global
    public TenantClient(TenantClientOptions tenantClientOptions,
        ITenantClientAccessToken tenantClientAccessToken)
    {
        AccessToken = tenantClientAccessToken;
        Options = tenantClientOptions;
    }

    // ReSharper disable once MemberCanBePrivate.Global
    /// <summary>
    /// Returns the GraphQL HTTP client.
    /// </summary>
    protected GraphQLHttpClient Client
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

    /// <inheritdoc />
    public IServiceClientAccessToken AccessToken { get; }

    /// <inheritdoc />
    public TenantClientOptions Options { get; }

    /// <inheritdoc />
    public Uri ServiceUri
    {
        get
        {
            if (_serviceUri == null)
            {
                if (string.IsNullOrWhiteSpace(Options.EndpointUri))
                {
                    throw new ServiceConfigurationMissingException("Asset Repository Service URI is not configured.");
                }

                if (string.IsNullOrWhiteSpace(Options.TenantId))
                {
                    throw new ServiceConfigurationMissingException("TenantId is not configured.");
                }

                _serviceUri = new Uri(Options.EndpointUri).Append("tenants/")
                    .Append(Options.TenantId).Append("GraphQL");
            }

            return _serviceUri;
        }
    }

    /// <inheritdoc />
    public HttpClient HttpClient => Client.HttpClient;

    /// <inheritdoc />
    public async Task<QlItemsContainer<TDto>?> SendQueryAsync<TDto>(GraphQLRequest query) where TDto : class
    {
        try
        {
            var result = await Client.SendQueryAsync<QlQueryResponse<TDto>>(query);
            CheckResult(result);

            return result.Data.Connection;
        }
        catch (GraphQLHttpRequestException e)
        {
            if (e.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedServiceAccessException(e);
            }

            throw new ServiceClientException("Call to GraphQL source failed.", e);
        }
    }

    /// <inheritdoc />
    public async Task<TDto> SendMutationAsync<TDto>(GraphQLRequest query)
    {
        try
        {
            var result = await Client.SendMutationAsync<QlMutationResponse<TDto>>(query);
            CheckResult(result);

            return result.Data.Result!;
        }
        catch (GraphQLHttpRequestException e)
        {
            if (e.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedServiceAccessException(e);
            }

            throw new ServiceClientException("Call to GraphQL source failed.", e);
        }
    }

    private GraphQLHttpClient CreateClient()
    {
      
        var client = new GraphQLHttpClient(ServiceUri, new NewtonsoftJsonSerializer());

        AccessToken.AccessTokenUpdated += (_, _) => UpdateAccessToken(AccessToken.AccessToken);

        return client;
    }

    private void UpdateAccessToken(string? accessToken)
    {
        HttpClient.DefaultRequestHeaders.Remove("Authorization");
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"bearer {accessToken}");
        }
    }

    private static void CheckResult<TResponse>(GraphQLResponse<TResponse> result) where TResponse : class
    {
        if (result.Errors != null)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("An error occurred in query definition:");
            foreach (var error in result.Errors)
            {
                stringBuilder.AppendLine(error.Message);
                stringBuilder.AppendLine("Location");
                if (error.Path != null)
                {
                    foreach (var path in error.Path)
                    {
                        stringBuilder.AppendLine(path.ToString());
                    }
                }

                if (error.Locations != null)
                {
                    foreach (var location in error.Locations)
                    {
                        stringBuilder.AppendLine($"at {location.Line},{location.Column}:");
                    }
                }

                stringBuilder.AppendLine("----");
            }

            throw new QlQueryErrorException(stringBuilder.ToString());
        }
    }
}
