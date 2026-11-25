using FakeItEasy;
using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.System;
using Sdk.ServiceClient.Tests.BotServices;

namespace Sdk.ServiceClient.Tests.AssetRepositoryServices.Blueprints;

/// <summary>
///     <see cref="AssetServicesClient.ValidateBlueprintAsync" /> posts the draft as the multipart member
///     <c>file</c> to <c>blueprints/validate</c> and reads the findings with their stable codes.
/// </summary>
public class ValidateBlueprintTests : IClassFixture<LoopbackHttpService>
{
    private const string InvalidBody = """
        {
          "isValid": false,
          "blueprintId": "Draft-1.0.0",
          "totalFindings": 1,
          "truncated": false,
          "findings": [{
            "code": 163, "level": "Error", "file": "blueprint.yaml", "modelId": "Acme.Unknown",
            "message": "CK model dependency 'Acme.Unknown' in range '[7.0,8.0)' resolves in none of the CK model catalogs (all)",
            "parameters": { "modelId": "Acme.Unknown", "versionRange": "[7.0,8.0)", "catalogs": "all" }
          }]
        }
        """;

    private readonly LoopbackHttpService _service;
    private readonly AssetServicesClient _client;

    public ValidateBlueprintTests(LoopbackHttpService service)
    {
        _service = service;
        _service.Reset();
        _client = new AssetServicesClient(new AssetServiceClientOptions { EndpointUri = _service.BaseUrl },
            A.Fake<IAssetServiceClientAccessToken>());
    }

    private static string DraftFile(string extension)
    {
        var path = Path.Combine(Path.GetTempPath(), $"ab9371-{Guid.NewGuid():N}{extension}");
        File.WriteAllText(path, "blueprintId: Draft-1.0.0");
        return path;
    }

    [Fact]
    public async Task ValidateBlueprintAsync_PostsTheFile_AndReadsTheFindings()
    {
        _service.RespondWith("/acme/v1/blueprints/validate", 200, InvalidBody);
        var file = DraftFile(".yaml");
        try
        {
            var result = await _client.ValidateBlueprintAsync("acme", file);

            Assert.Equal("POST /acme/v1/blueprints/validate", _service.SingleRequest());
            var body = Assert.Single(_service.Bodies);
            Assert.Matches("name=\"?file\"?", body);
            Assert.Contains("blueprintId: Draft-1.0.0", body);

            Assert.False(result.IsValid);
            Assert.Equal("Draft-1.0.0", result.BlueprintId);
            var finding = Assert.Single(result.Findings);
            Assert.Equal(163, finding.Code);
            Assert.Equal("Acme.Unknown", finding.ModelId);
            Assert.Equal("[7.0,8.0)", finding.Parameters["versionRange"]);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public async Task ValidateBlueprintAsync_AnUnsupportedExtension_IsRefusedBeforeAnyRequest()
    {
        var file = DraftFile(".json");
        try
        {
            await Assert.ThrowsAsync<ServiceClientException>(() => _client.ValidateBlueprintAsync("acme", file));
            Assert.Empty(_service.Requests);
        }
        finally
        {
            File.Delete(file);
        }
    }
}
