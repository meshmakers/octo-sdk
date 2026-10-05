using System.Buffers;
using System.Buffers.Text;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using GraphQL;
using GraphQL.Client.Http;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.Runtime.Contracts.RepositoryEntities;
using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;

namespace Sdk.ServiceClient.Tests.Secrets;

/// <summary>
///     AB#5534 (WP4 of AB#5528): the serializers the SDK clients build write an RtSecretValue as the
///     marker <c>{"isSet":true}</c>, and the GraphQL client sends <c>clearSecretAttributes</c> only
///     when set.
/// </summary>
public class SecretSerializationWiringTests
{
    private static readonly string Envelope =
        "enc:v2:k1:" + Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(48));

    [Fact]
    public void TenantClientGraphQlSerializer_WritesSecretMarker_AndClearSecretAttributes()
    {
        var client = new ExposedTenantClient();
        var request = new GraphQLRequest
        {
            Query = "mutation { x }",
            Variables = new
            {
                entities = new[]
                {
                    new MutationDto<RtEntityDto>
                    {
                        RtId = OctoObjectId.GenerateNewId(),
                        Item = new RtEntityDto(),
                        ClearSecretAttributes = ["password"]
                    }
                },
                stray = RtSecretValue.Protected(Envelope)
            }
        };

        var json = client.GraphQlClient.JsonSerializer.SerializeToString(request);

        Assert.Contains("\"clearSecretAttributes\":[\"password\"]", json, StringComparison.Ordinal);
        Assert.Contains("\"stray\":{\"isSet\":true}", json, StringComparison.Ordinal);
        Assert.DoesNotContain("enc:", json, StringComparison.Ordinal);
    }

    [Fact]
    public void TenantClientGraphQlSerializer_OmitsClearSecretAttributesWhenNull()
    {
        var client = new ExposedTenantClient();
        var request = new GraphQLRequest
        {
            Query = "mutation { x }",
            Variables = new
            {
                entities = new[] { new MutationDto<RtEntityDto> { RtId = OctoObjectId.GenerateNewId(), Item = new RtEntityDto() } }
            }
        };

        var json = client.GraphQlClient.JsonSerializer.SerializeToString(request);

        Assert.DoesNotContain("clearSecretAttributes", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SignalRHubProtocol_WritesSecretMarker()
    {
        var client = new ExposedSignalRClient();
        try
        {
            var protocol = FindHubProtocol(client.GetHubConnection());
            Assert.NotNull(protocol);

            var buffer = new ArrayBufferWriter<byte>();
            protocol!.WriteMessage(new InvocationMessage("Target", [RtSecretValue.Protected(Envelope)]), buffer);
            var payload = Encoding.UTF8.GetString(buffer.WrittenSpan);

            Assert.Contains("{\"isSet\":true}", payload, StringComparison.Ordinal);
            Assert.DoesNotContain("enc:", payload, StringComparison.Ordinal);
        }
        finally
        {
            await client.StopAsync();
        }
    }

    private static IHubProtocol? FindHubProtocol(HubConnection connection)
    {
        return connection.GetType()
            .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .Select(f => f.GetValue(connection))
            .OfType<IHubProtocol>()
            .FirstOrDefault();
    }

    private sealed class ExposedTenantClient()
        : TenantClient(new TenantClientOptions { EndpointUri = "https://localhost:5001", TenantId = "test" },
            new ServiceClientAccessToken())
    {
        public GraphQLHttpClient GraphQlClient => Client;
    }

    private sealed class ExposedSignalRClient()
        : SignalRClient<SignalRClientOptions>(
            new SignalRClientOptions { EndpointUri = "https://localhost:5015", TenantId = "testTenant" },
            A.Fake<ILogger<SignalRClient<SignalRClientOptions>>>(), new ServiceClientAccessToken(), "testHub")
    {
        public HubConnection GetHubConnection() => HubConnection;
    }
}
