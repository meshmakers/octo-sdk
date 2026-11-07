using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.System;
using Sdk.ServiceClient.Tests.BotServices;

namespace Sdk.ServiceClient.Tests.AssetRepositoryServices.CkModelCatalog;

/// <summary>
///     AB#8492: <see cref="AssetServicesClient.ValidateCkModelAsync" /> posts the draft as the multipart member
///     <c>file</c> to <c>models/ValidateCk</c> and reads the validation result.
/// </summary>
public class ValidateCkModelTests : IClassFixture<LoopbackHttpService>
{
    private const string InvalidBody = """
        {
          "isValid": false,
          "modelId": "Demo-1.0.0",
          "sourceKind": "Source",
          "errors": [{
            "messageNumber": 1, "level": "Error", "file": "ckModel.yaml", "elementPath": "Basic-[2.0,3.0)",
            "message": "'Basic-[2.0,3.0)' is not a known construction kit model.", "parameters": { "modelId": "Basic-[2.0,3.0)" }
          }],
          "warnings": [],
          "isBlocked": false,
          "blockers": []
        }
        """;

    private readonly LoopbackHttpService _service;
    private readonly AssetServicesClient _client;

    public ValidateCkModelTests(LoopbackHttpService service)
    {
        _service = service;
        _service.Reset();
        _client = new AssetServicesClient(new AssetServiceClientOptions { EndpointUri = _service.BaseUrl },
            A.Fake<IAssetServiceClientAccessToken>());
    }

    private static string DraftFile(string extension)
    {
        var path = Path.Combine(Path.GetTempPath(), $"ab8492-{Guid.NewGuid():N}{extension}");
        File.WriteAllText(path, "modelId: Demo-1.0.0");
        return path;
    }

    [Fact]
    public async Task ValidateCkModelAsync_PostsTheFile_AndReadsTheErrors()
    {
        _service.RespondWith("/acme/v1/models/ValidateCk", 200, InvalidBody);
        var file = DraftFile(".yaml");
        try
        {
            var result = await _client.ValidateCkModelAsync("acme", file);

            Assert.Equal("POST /acme/v1/models/ValidateCk", _service.SingleRequest());
            var body = Assert.Single(_service.Bodies);
            Assert.Matches("name=\"?file\"?", body);
            Assert.Contains("modelId: Demo-1.0.0", body);

            Assert.False(result.IsValid);
            Assert.Equal("Demo-1.0.0", result.ModelId);
            Assert.Equal("Source", result.SourceKind);
            var error = Assert.Single(result.Errors);
            Assert.Equal(1, error.MessageNumber);
            Assert.Equal("ckModel.yaml", error.File);
            Assert.Equal("Basic-[2.0,3.0)", error.ElementPath);
            Assert.Equal("Basic-[2.0,3.0)", error.Parameters["modelId"]);
            Assert.False(result.IsBlocked);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public async Task ValidateCkModelAsync_AValidButBlockedDraft_ReadsTheBlockers()
    {
        _service.RespondWith("/acme/v1/models/ValidateCk", 200, """
            {
              "isValid": true,
              "errors": [],
              "isBlocked": true,
              "blockers": [{ "dependent": "EnergyCommunity.Registration-2.8.0", "code": "CK_UPGRADE_EXACT_PIN" }]
            }
            """);
        var file = DraftFile(".yaml");
        try
        {
            var result = await _client.ValidateCkModelAsync("acme", file);

            Assert.True(result.IsValid);
            Assert.True(result.IsBlocked);
            var blocker = Assert.Single(result.Blockers);
            Assert.Equal("EnergyCommunity.Registration-2.8.0", blocker.Dependent);
            Assert.Equal("CK_UPGRADE_EXACT_PIN", blocker.Code);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public async Task ValidateCkModelAsync_AnUnsupportedExtension_IsRefusedBeforeAnyRequest()
    {
        var file = DraftFile(".txt");
        try
        {
            await Assert.ThrowsAsync<ServiceClientException>(() => _client.ValidateCkModelAsync("acme", file));
            Assert.Empty(_service.Requests);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public async Task ValidateCkModelAsync_AMissingFile_IsRefusedBeforeAnyRequest()
    {
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _client.ValidateCkModelAsync("acme", Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.zip")));
        Assert.Empty(_service.Requests);
    }

    [Fact]
    public async Task ValidateCkModelAsync_ABadRequest_Throws_InsteadOfAnEmptyResult()
    {
        _service.RespondWith("/acme/v1/models/ValidateCk", 400, """{"message":"A CK model file is required."}""");
        var file = DraftFile(".zip");
        try
        {
            var ex = await Assert.ThrowsAnyAsync<Exception>(() => _client.ValidateCkModelAsync("acme", file));
            Assert.Contains("BadRequest", ex.ToString());
        }
        finally
        {
            File.Delete(file);
        }
    }
}
