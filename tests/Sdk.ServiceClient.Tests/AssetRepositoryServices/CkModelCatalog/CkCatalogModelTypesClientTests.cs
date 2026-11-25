using System.Net;
using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.System;
using Sdk.ServiceClient.Tests.TestSupport;

namespace Sdk.ServiceClient.Tests.AssetRepositoryServices.CkModelCatalog;

/// <summary>
///     The SDK reads a catalog model's types from GET system/v1/ckmodelcatalog/{catalog}/{modelId}/types and
///     passes the stable problem code of a refusal through.
/// </summary>
public sealed class CkCatalogModelTypesClientTests : IDisposable
{
    private readonly WireService _service = new();

    public void Dispose() => _service.Dispose();

    [Fact]
    public async Task GetTypes_CallsTheTypesRoute_AndReadsThePage()
    {
        _service.Respond(200, """
            {"catalogName":"PublicGitHubCatalog","modelId":"Basic-2.7.0","totalCount":1,"modelTypeCount":9,"skip":0,"take":5,
             "types":[{"typeId":"Basic/Equipment","attributes":[{"name":"State","attributeId":"Basic/EquipmentState","valueType":"Enum","enumId":"Basic/EquipmentState"}],
                       "associations":[{"roleId":"Basic/LocatedAt","target":"Basic/Location"}]}],
             "associationRoles":[{"roleId":"Basic/LocatedAt","inboundName":"Equipment","outboundName":"Location","inboundMultiplicity":"N","outboundMultiplicity":"ZeroOrOne"}],
             "enums":[{"enumId":"Basic/EquipmentState","values":[{"key":0,"name":"Running"}]}]}
            """);

        var page = await NewClient().GetCkCatalogModelTypesAsync("PublicGitHubCatalog", "Basic-2.7.0", "location", 0, 5);

        Assert.Equal("GET /system/v1/ckmodelcatalog/PublicGitHubCatalog/Basic-2.7.0/types?filter=location&skip=0&take=5",
            _service.SingleRequest());
        Assert.Equal(9, page.ModelTypeCount);
        var type = Assert.Single(page.Types);
        Assert.Equal("Basic/Equipment", type.TypeId);
        Assert.Equal("Enum", Assert.Single(type.Attributes).ValueType);
        Assert.Equal("Basic/Location", Assert.Single(type.Associations).Target);
        Assert.Equal("ZeroOrOne", Assert.Single(page.AssociationRoles).OutboundMultiplicity);
        Assert.Equal("Running", Assert.Single(Assert.Single(page.Enums).Values).Name);
    }

    [Fact]
    public async Task GetTypes_UnknownModel_ThrowsWithTheProblemCode()
    {
        _service.Respond(404, """{"title":"CK model not found in catalog","status":404,"detail":"x","code":"CkCatalogModelNotFound"}""");

        var error = await Assert.ThrowsAsync<ServiceClientResultException>(
            () => NewClient().GetCkCatalogModelTypesAsync("PublicGitHubCatalog", "Basic-9.9.9"));

        Assert.Equal(HttpStatusCode.NotFound, error.HttpStatusCode);
        Assert.True(ServiceProblem.TryParse(error, out var problem));
        Assert.Equal("CkCatalogModelNotFound", problem.Code);
        Assert.Equal("GET /system/v1/ckmodelcatalog/PublicGitHubCatalog/Basic-9.9.9/types?skip=0&take=20",
            _service.SingleRequest());
    }

    private AssetServicesClient NewClient() =>
        new(new AssetServiceClientOptions { EndpointUri = _service.BaseUrl }, A.Fake<IAssetServiceClientAccessToken>());
}
