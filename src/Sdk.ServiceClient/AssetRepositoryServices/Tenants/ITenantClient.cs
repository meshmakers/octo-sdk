using GraphQL;

namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;

/// <summary>
///     Interface for the <see cref="TenantClient" />.
/// </summary>
public interface ITenantClient : IServiceClient
{
    /// <summary>
    ///     Options for the <see cref="TenantClient" />.
    /// </summary>
    TenantClientOptions Options { get; }

    /// <summary>
    ///     The HTTP client used to send requests.
    /// </summary>
    HttpClient HttpClient { get; }

    /// <summary>
    ///     Sends a query to the GraphQL endpoint.
    /// </summary>
    /// <param name="query">The GraphQL query</param>
    /// <typeparam name="TDto">The data transfer object used to read the response</typeparam>
    /// <returns></returns>
    Task<QlItemsContainer<TDto>?> SendQueryAsync<TDto>(GraphQLRequest query) where TDto : class;

    /// <summary>
    ///     Sends a mutation to the GraphQL endpoint.
    /// </summary>
    /// <param name="query"></param>
    /// <typeparam name="TDto"></typeparam>
    /// <returns></returns>
    Task<TDto> SendMutationAsync<TDto>(GraphQLRequest query);
}