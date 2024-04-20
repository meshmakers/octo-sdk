using GraphQL;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.Runtime.Contracts;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;
using Microsoft.Extensions.DependencyInjection;
using Sdk.ServiceClient.SystemTests.Fixtures;
using Sdk.ServiceClient.SystemTests.Models;

namespace Sdk.ServiceClient.SystemTests;

public class TenantClientTests(ServiceCollectionFixture fixture) : IClassFixture<ServiceCollectionFixture>
{
    [Fact]
    public async Task Test1()
    {
        fixture.UseRepositoryClient();
        var tenantClient = fixture.Services.BuildServiceProvider().GetRequiredService<ITenantClient>();
        var query = new GraphQLRequest
        {
            Query = GraphQl.GetWalletsWithSubscriptions,
            Variables = new { }
        };
        var walletDtos = await tenantClient.SendQueryAsync<WalletDto>(query);
    }

    [Fact]
    public async Task Test2()
    {
        fixture.UseRepositoryClient();

        var updates = new List<MutationDto<WalletDto>>(); 
        var z = new WalletDto{ LastNotificationUpdate = DateTime.UtcNow };
        var x = new MutationDto<WalletDto>()
            { RtId = new("661bf1422d429f0376d45d97"), Item = z };
        updates.Add(x);

        var tenantClient = fixture.Services.BuildServiceProvider().GetRequiredService<ITenantClient>();
        var query = new GraphQLRequest
        {
            Query = GraphQl.UpdateWalletNotificationUpdateDateTimeMutation,
            Variables = new { updates }
        };
        var walletDtos = await tenantClient.SendMutationAsync<WalletDto>(query);
    }

    [Fact]
    public async Task Test3()
    {
        fixture.UseRepositoryClient();
        var updates = new List<MutationDto<WalletDto>>
        {
            new() { RtId = new("661bf1422d429f0376d45d97"), Item = new WalletDto{ LastNotificationUpdate = DateTime.UtcNow } },
            new() { RtId = new("6623a609357b40bc04326701"), Item = new WalletDto{ LastNotificationUpdate = DateTime.UtcNow } },
        };

        var tenantClient = fixture.Services.BuildServiceProvider().GetRequiredService<ITenantClient>();
        var query = new GraphQLRequest
        {
            Query = GraphQl.UpdateWalletNotificationUpdateDateTimeMutation,
            Variables = new { updates }
        };
        var walletDtos = await tenantClient.SendMutationsAsync<WalletDto>(query);
    }
}