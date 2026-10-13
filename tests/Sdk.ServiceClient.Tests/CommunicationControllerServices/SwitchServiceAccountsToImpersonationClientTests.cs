using System.Net;
using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;

namespace Sdk.ServiceClient.Tests.CommunicationControllerServices;

/// <summary>
///     AB#6642 — wire behaviour of <c>SwitchServiceAccountsToImpersonationAsync</c>: route, query
///     parameters, report mapping, and what a 409 / an empty 2xx become.
/// </summary>
public class SwitchServiceAccountsToImpersonationClientTests : IClassFixture<AdapterPoolQueueLoopbackService>
{
    private const string Report = """
        {
          "tenantId": "acme",
          "applied": false,
          "accounts": [
            {"configurationRtId":"68c1a2b3c4d5e6f701020304","wellKnownName":"pipeline-sa-1","clientId":"octo-sa-1",
             "outcome":"WouldSwitch","message":"would switch","pipelineRtIds":["p1","p2"]},
            {"configurationRtId":"68c1a2b3c4d5e6f701020305","wellKnownName":"pipeline-sa-2","clientId":null,
             "outcome":"SkippedIncomplete","message":"no client","pipelineRtIds":[]}
          ],
          "redeployedDataFlowRtIds": ["df1"],
          "failedDataFlowRedeploys": [{"dataFlowRtId":"df2","reason":"InvalidOperationException: boom"}]
        }
        """;

    private readonly AdapterPoolQueueLoopbackService _service;

    public SwitchServiceAccountsToImpersonationClientTests(AdapterPoolQueueLoopbackService service)
    {
        _service = service;
        _service.Reset();
    }

    private CommunicationServicesClient CreateClient()
    {
        var options = new CommunicationServiceClientOptions { EndpointUri = _service.BaseUrl, TenantId = "acme" };
        return new CommunicationServicesClient(options, A.Fake<ICommunicationServiceClientAccessToken>());
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public async Task PostsToRouteWithBothQueryParameters(bool apply, bool redeploy)
    {
        _service.Respond(HttpStatusCode.OK, Report);

        await CreateClient().SwitchServiceAccountsToImpersonationAsync(apply, redeploy);

        Assert.Equal(
            $"POST /acme/v1/serviceAccount/switchToImpersonation?apply={apply.ToString().ToLowerInvariant()}" +
            $"&redeployDataFlows={redeploy.ToString().ToLowerInvariant()}",
            _service.SingleRequest());
    }

    [Fact]
    public async Task ReturnsTheReport()
    {
        _service.Respond(HttpStatusCode.OK, Report);

        var report = await CreateClient().SwitchServiceAccountsToImpersonationAsync(false, true);

        Assert.Equal("acme", report.TenantId);
        Assert.False(report.Applied);
        Assert.Equal(2, report.Accounts.Count);
        Assert.Equal("WouldSwitch", report.Accounts[0].Outcome);
        Assert.Equal(["p1", "p2"], report.Accounts[0].PipelineRtIds);
        Assert.Null(report.Accounts[1].ClientId);
        Assert.Equal(["df1"], report.RedeployedDataFlowRtIds);
        Assert.Equal("df2", Assert.Single(report.FailedDataFlowRedeploys).DataFlowRtId);
    }

    [Fact]
    public async Task EmptySuccessBody_ThrowsServiceClientException()
    {
        _service.Respond(HttpStatusCode.OK);

        await Assert.ThrowsAsync<ServiceClientException>(
            () => CreateClient().SwitchServiceAccountsToImpersonationAsync(true, true));
    }

    [Fact]
    public async Task Conflict_SurfacesAsServiceClientResultExceptionWith409()
    {
        _service.Respond(HttpStatusCode.Conflict, "secret clearing not allowed");

        var ex = await Assert.ThrowsAsync<ServiceClientResultException>(
            () => CreateClient().SwitchServiceAccountsToImpersonationAsync(true, true));

        Assert.Equal(HttpStatusCode.Conflict, ex.HttpStatusCode);
    }
}
