using System.Net.Http;
using System.Threading.Tasks;
using GraphQL;

namespace Meshmakers.Octo.Frontend.Client.Tenants;

public interface ITenantClient : IServiceClient
{
    TenantClientOptions Options { get; }

    HttpClient HttpClient { get; }

    Task<QlItemsContainer<TDto>> SendQueryAsync<TDto>(GraphQLRequest query) where TDto : class;
    Task<TDto> SendMutationAsync<TDto>(GraphQLRequest query);
}
