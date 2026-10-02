using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.CkModelCatalog;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.System;
using Sdk.ServiceClient.Tests.BotServices;

namespace Sdk.ServiceClient.Tests.AssetRepositoryServices.CkModelCatalog;

public class ImportFromCatalogBatchTests : IClassFixture<LoopbackHttpService>
{
    private readonly LoopbackHttpService _service;

    public ImportFromCatalogBatchTests(LoopbackHttpService service)
    {
        _service = service;
        _service.Reset();
    }

    [Fact]
    public async Task ImportFromCatalogBatchAsync_ReadsTheJobIdTheServiceReturns()
    {
        var client = new AssetServicesClient(new AssetServiceClientOptions { EndpointUri = _service.BaseUrl },
            A.Fake<IAssetServiceClientAccessToken>());

        var result = await client.ImportFromCatalogBatchAsync("acme",
            new ImportFromCatalogBatchRequestDto { CatalogName = "PublicGitHubCatalog", ModelIds = ["Basic-2.1.0"] });

        Assert.Equal("POST /acme/v1/models/ImportFromCatalogBatch", _service.SingleRequest());
        Assert.Equal("job-1", result.JobId);
    }
}
