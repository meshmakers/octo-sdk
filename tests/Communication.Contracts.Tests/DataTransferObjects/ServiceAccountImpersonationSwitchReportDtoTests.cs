using System.Text.Json;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

namespace Communication.Contracts.Tests.DataTransferObjects;

/// <summary>
///     AB#6642 — the SDK mirror of the controller's impersonation switch report (AB#5537).
/// </summary>
public class ServiceAccountImpersonationSwitchReportDtoTests
{
    private const string ControllerWireShape =
        """
        {
          "tenantId": "acme",
          "applied": true,
          "accounts": [
            {"configurationRtId":"a1","wellKnownName":"sa","clientId":"octo-sa","outcome":"SwitchedPendingInvalidation",
             "message":"switched","pipelineRtIds":["p1"]}
          ],
          "redeployedDataFlowRtIds": ["df1"],
          "failedDataFlowRedeploys": [{"dataFlowRtId":"df2","reason":"boom"}]
        }
        """;

    [Fact]
    public void DeserialisesTheControllerWireShape()
    {
        var dto = JsonSerializer.Deserialize<ServiceAccountImpersonationSwitchReportDto>(
            ControllerWireShape, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(dto);
        Assert.Equal("acme", dto!.TenantId);
        Assert.True(dto.Applied);
        var account = Assert.Single(dto.Accounts);
        Assert.Equal("a1", account.ConfigurationRtId);
        Assert.Equal("sa", account.WellKnownName);
        Assert.Equal("octo-sa", account.ClientId);
        Assert.Equal(ServiceAccountImpersonationSwitchOutcomes.SwitchedPendingInvalidation, account.Outcome);
        Assert.Equal(["p1"], account.PipelineRtIds);
        Assert.Equal(["df1"], dto.RedeployedDataFlowRtIds);
        Assert.Equal("boom", Assert.Single(dto.FailedDataFlowRedeploys).Reason);
    }

    [Fact]
    public void CarriesNoSecretOrTokenShapedMember()
    {
        var members = new[]
            {
                typeof(ServiceAccountImpersonationSwitchReportDto),
                typeof(ServiceAccountImpersonationSwitchEntryDto),
                typeof(DataFlowRedeployFailureDto)
            }
            .SelectMany(t => t.GetProperties())
            .Select(p => p.Name)
            .ToList();

        Assert.DoesNotContain(members, n => n.Contains("Secret", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(members, n => n.Contains("Token", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(members, n => n.Contains("Password", StringComparison.OrdinalIgnoreCase));
    }
}
