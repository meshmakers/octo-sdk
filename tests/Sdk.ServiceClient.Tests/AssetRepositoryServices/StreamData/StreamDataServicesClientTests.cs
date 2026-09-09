using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.StreamData;
using RestSharp;
using RestSharp.Serializers;
using RestSharp.Serializers.Json;

namespace Sdk.ServiceClient.Tests.AssetRepositoryServices.StreamData;

public class StreamDataServicesClientTests
{
    private static StreamDataServicesClient CreateClient(string? endpointUri, string? tenantId)
    {
        var options = new StreamDataServiceClientOptions
        {
            EndpointUri = endpointUri,
            TenantId = tenantId
        };
        var accessToken = A.Fake<IStreamDataServiceClientAccessToken>();
        return new StreamDataServicesClient(options, accessToken);
    }

    [Fact]
    public void ServiceUri_WithTenantId_ReturnsTenantScopedUri()
    {
        var client = CreateClient("https://asset.example.com", "acme");

        Assert.Equal("https://asset.example.com/acme/v1", client.ServiceUri.ToString());
    }

    [Fact]
    public void ServiceUri_WithTrailingSlash_ReturnsTenantScopedUri()
    {
        var client = CreateClient("https://asset.example.com/", "acme");

        Assert.Equal("https://asset.example.com/acme/v1", client.ServiceUri.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ServiceUri_BlankTenantId_ThrowsServiceConfigurationMissingException(string? tenantId)
    {
        var client = CreateClient("https://asset.example.com", tenantId);

        var exception = Assert.Throws<ServiceConfigurationMissingException>(() => client.ServiceUri);
        Assert.Contains("tenant ID", exception.Message);
    }

    [Fact]
    public void ServiceUri_MissingEndpointUri_ThrowsServiceConfigurationMissingException()
    {
        var client = CreateClient(null, "acme");

        var exception = Assert.Throws<ServiceConfigurationMissingException>(() => client.ServiceUri);
        Assert.Contains("URI is missing", exception.Message);
    }

    // ---- Rollup listing payload (AB#5157) ----
    // Pinned through RestSharp's own default serializer, the one ListRollupsForArchiveAsync
    // deserialises with, so the DTO stays faithful to the wire shape.
    private static T? DeserializeAsClient<T>(string json)
    {
        IDeserializer serializer = new SystemTextJsonSerializer();
        var response = new RestResponse(new RestRequest()) { Content = json };
        return serializer.Deserialize<T>(response);
    }

    [Fact]
    public void RollupArchiveInfo_WithSources_DeserialisesEverySourceSpan()
    {
        const string json = """
                            [
                              {
                                "rtId": "6700000000000000000000a1",
                                "rtWellKnownName": "hourly",
                                "status": "Activated",
                                "sourceArchiveRtId": null,
                                "bucketSizeMs": 3600000,
                                "watermarkLagMs": 60000,
                                "lastAggregatedBucketEnd": "2026-09-09T10:00:00Z",
                                "frozenUntil": null,
                                "aggregationCount": 2,
                                "recomputeInProgress": false,
                                "lastRecomputeStartedAt": null,
                                "lastRecomputeSuccessAt": null,
                                "lastRecomputeFailureAt": null,
                                "lastRecomputeFailureReason": null,
                                "dirtyWindowsPending": 0,
                                "pendingRecomputeRanges": 0,
                                "sources": [
                                  {
                                    "sourceArchiveRtId": "6700000000000000000000b1",
                                    "validFrom": "2025-01-01T00:00:00Z",
                                    "validTo": "2026-01-01T00:00:00Z"
                                  },
                                  {
                                    "sourceArchiveRtId": "6700000000000000000000b2",
                                    "validFrom": null,
                                    "validTo": null
                                  }
                                ]
                              }
                            ]
                            """;

        var rollups = DeserializeAsClient<List<RollupArchiveInfoDto>>(json);

        var rollup = Assert.Single(rollups!);
        Assert.Null(rollup.SourceArchiveRtId);
        Assert.NotNull(rollup.Sources);
        Assert.Equal(2, rollup.Sources!.Count);

        var bounded = rollup.Sources[0];
        Assert.Equal("6700000000000000000000b1", bounded.SourceArchiveRtId);
        Assert.Equal(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), bounded.ValidFrom!.Value.ToUniversalTime());
        Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), bounded.ValidTo!.Value.ToUniversalTime());

        var unbounded = rollup.Sources[1];
        Assert.Equal("6700000000000000000000b2", unbounded.SourceArchiveRtId);
        Assert.Null(unbounded.ValidFrom);
        Assert.Null(unbounded.ValidTo);
    }

    [Fact]
    public void RollupArchiveInfo_WithoutSources_LeavesSourcesNullAndKeepsLegacySourceArchiveRtId()
    {
        // A server predating System.StreamData 1.8.0 omits "sources" entirely.
        const string json = """
                            [
                              {
                                "rtId": "6700000000000000000000a1",
                                "rtWellKnownName": null,
                                "status": "Activated",
                                "sourceArchiveRtId": "6700000000000000000000b1",
                                "bucketSizeMs": 3600000,
                                "watermarkLagMs": 60000,
                                "lastAggregatedBucketEnd": null,
                                "frozenUntil": null,
                                "aggregationCount": 1,
                                "recomputeInProgress": false,
                                "lastRecomputeStartedAt": null,
                                "lastRecomputeSuccessAt": null,
                                "lastRecomputeFailureAt": null,
                                "lastRecomputeFailureReason": null,
                                "dirtyWindowsPending": 0,
                                "pendingRecomputeRanges": 0
                              }
                            ]
                            """;

        var rollups = DeserializeAsClient<List<RollupArchiveInfoDto>>(json);

        var rollup = Assert.Single(rollups!);
        Assert.Null(rollup.Sources);
        Assert.Equal("6700000000000000000000b1", rollup.SourceArchiveRtId);
    }
}
