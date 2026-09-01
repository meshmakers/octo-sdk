using System.Text.Json;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

namespace Communication.Contracts.Tests.DataTransferObjects;

/// <summary>
///     AB#5048 — the SDK mirror of the communication controller's rotation result
///     (<c>POST {tenantId}/v1/adapter/{adapterRtId}/serviceAccount/rotateSecret</c>, AB#5032).
///     The controller pins "no secret in the response" with a test of its own; this is the same
///     pin on the client side, so a future convenience property cannot reintroduce a third copy of
///     the plaintext through the SDK.
/// </summary>
public class RotateServiceAccountSecretResultDtoTests
{
    /// <summary>Exactly what the controller serialises, with ASP.NET's camelCase naming.</summary>
    private const string ControllerWireShape =
        """
        {
          "clientId": "octo-pipeline-sa-1",
          "configurationWellKnownName": "pipeline-service-account-1",
          "wasCreated": false,
          "requiresPipelineRedeploy": true,
          "message": "The client secret of pipeline service account 'octo-pipeline-sa-1' was rotated. Redeploy the pipelines / data flows of this adapter."
        }
        """;

    [Fact]
    public void CarriesNoSecretShapedMember()
    {
        var members = typeof(RotateServiceAccountSecretResultDto).GetProperties()
            .Select(p => p.Name)
            .ToList();

        Assert.DoesNotContain(members, n => n.Contains("Secret", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DeserialisesTheControllerWireShape()
    {
        var dto = JsonSerializer.Deserialize<RotateServiceAccountSecretResultDto>(
            ControllerWireShape, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(dto);
        Assert.Equal("octo-pipeline-sa-1", dto!.ClientId);
        Assert.Equal("pipeline-service-account-1", dto.ConfigurationWellKnownName);
        Assert.False(dto.WasCreated);
        // The flag the CLI turns into the redeploy hint — a mapping slip here would silently
        // downgrade every rotation to "nothing left to do".
        Assert.True(dto.RequiresPipelineRedeploy);
        Assert.Contains("Redeploy", dto.Message);
    }

    [Fact]
    public void FirstProvisioningIsReportedAsNotRequiringARedeploy()
    {
        const string wireShape =
            """
            {
              "clientId": "octo-pipeline-sa-1",
              "configurationWellKnownName": "pipeline-service-account-1",
              "wasCreated": true,
              "requiresPipelineRedeploy": false,
              "message": "Adapter 'mesh-adapter' had no pipeline service account; one was provisioned instead. Nothing was invalidated."
            }
            """;

        var dto = JsonSerializer.Deserialize<RotateServiceAccountSecretResultDto>(
            wireShape, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(dto);
        Assert.True(dto!.WasCreated);
        Assert.False(dto.RequiresPipelineRedeploy);
    }
}
