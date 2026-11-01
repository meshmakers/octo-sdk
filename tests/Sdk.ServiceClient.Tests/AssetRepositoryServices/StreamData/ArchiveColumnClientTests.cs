using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.StreamData;
using Sdk.ServiceClient.Tests.BotServices;

namespace Sdk.ServiceClient.Tests.AssetRepositoryServices.StreamData;

/// <summary>
///     AB#7997 — adding an ingested column to an activated archive, pinned on the wire against a loopback service.
/// </summary>
public class ArchiveColumnClientTests : IClassFixture<LoopbackHttpService>
{
    private const string Archive = "6a0e000000000000000a000d";
    private readonly LoopbackHttpService _service;

    public ArchiveColumnClientTests(LoopbackHttpService service)
    {
        _service = service;
        _service.Reset();
    }

    [Fact]
    public async Task AddArchiveColumnAsync_PostsTheColumnsRoute_WithPathAndIndexed()
    {
        _service.RespondWith($"/acme/v1/streamdata/archives/{Archive}/columns", 204, string.Empty);

        await CreateClient().AddArchiveColumnAsync("acme", Archive, "DischargingPower", indexed: true);

        Assert.Equal($"POST /acme/v1/streamdata/archives/{Archive}/columns?path=DischargingPower&indexed=true",
            _service.SingleRequest());
    }

    [Fact]
    public async Task AddArchiveColumnAsync_DefaultsToNotIndexed()
    {
        _service.RespondWith($"/acme/v1/streamdata/archives/{Archive}/columns", 204, string.Empty);

        await CreateClient().AddArchiveColumnAsync("acme", Archive, "Amount.Value");

        Assert.Equal($"POST /acme/v1/streamdata/archives/{Archive}/columns?path=Amount.Value&indexed=false",
            _service.SingleRequest());
    }

    [Fact]
    public async Task AddArchiveColumnAsync_UnknownPath_SurfacesTheServerRefusal()
    {
        _service.RespondWith($"/acme/v1/streamdata/archives/{Archive}/columns", 400,
            "\"Archive '6a0e000000000000000a000d' path 'Nope' is invalid: attribute 'Nope' is not defined.\"");

        await Assert.ThrowsAnyAsync<Exception>(() => CreateClient().AddArchiveColumnAsync("acme", Archive, "Nope"));
    }

    private StreamDataServicesClient CreateClient()
    {
        var options = new StreamDataServiceClientOptions { EndpointUri = _service.BaseUrl, TenantId = "acme" };
        return new StreamDataServicesClient(options, A.Fake<IStreamDataServiceClientAccessToken>());
    }
}
