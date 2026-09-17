using System.Net;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;

namespace Sdk.ServiceClient.Tests.CommunicationControllerServices;

/// <summary>
///     AB#4924 §10 — the shared queue contract every surface consumes. What is pinned here is the
///     three things the endpoint decided: position travels as the tenant-local pair and never as a
///     global rank, a leased entry is part of the answer and is the only carrier of a member id, and
///     <c>DELETE</c> answers 409 for an execution that already holds a lease — an outcome, not an
///     exception, because interrupting a running pipeline is a different operation (concept §5).
/// </summary>
public class AdapterPoolQueueClientTests : IClassFixture<AdapterPoolQueueLoopbackService>
{
    private const string AdapterPoolRtId = "68c1a2b3c4d5e6f701020304";

    private readonly AdapterPoolQueueLoopbackService _service;

    public AdapterPoolQueueClientTests(AdapterPoolQueueLoopbackService service)
    {
        _service = service;
        _service.Reset();
    }

    private CommunicationServicesClient CreateClient()
    {
        var options = new CommunicationServiceClientOptions
        {
            EndpointUri = _service.BaseUrl,
            TenantId = "lender"
        };
        var accessToken = A.Fake<ICommunicationServiceClientAccessToken>();
        return new CommunicationServicesClient(options, accessToken);
    }

    [Fact]
    public async Task GetQueue_ReportsPositionInTenantAndTenantsAhead_NotAGlobalRank()
    {
        // Two borrowers with queued work. Under round-robin, borrower-b's first item runs before
        // borrower-a's second one — which is exactly what a single rank could not express.
        _service.Respond(HttpStatusCode.OK, """
            [
              {"executionId":"e-a1","borrowerTenantId":"borrower-a","pipelineRtId":"p1","pipelineName":"Nightly",
               "executionClass":1,"queuedAtUtc":"2026-09-14T08:00:00Z","positionInTenant":1,
               "tenantsAheadInRotation":0,"leasedOnMemberId":null,"leaseExpiresAtUtc":null},
              {"executionId":"e-a2","borrowerTenantId":"borrower-a","pipelineRtId":"p1","pipelineName":"Nightly",
               "executionClass":1,"queuedAtUtc":"2026-09-14T08:00:05Z","positionInTenant":2,
               "tenantsAheadInRotation":0,"leasedOnMemberId":null,"leaseExpiresAtUtc":null},
              {"executionId":"e-b1","borrowerTenantId":"borrower-b","pipelineRtId":"p2","pipelineName":"Billing",
               "executionClass":0,"queuedAtUtc":"2026-09-14T08:00:07Z","positionInTenant":1,
               "tenantsAheadInRotation":1,"leasedOnMemberId":null,"leaseExpiresAtUtc":null}
            ]
            """);
        var client = CreateClient();

        var queue = await client.GetAdapterPoolQueueAsync(AdapterPoolRtId);

        Assert.Equal($"GET /lender/v1/adapterPool/{AdapterPoolRtId}/queue", _service.SingleRequest());
        Assert.Equal(3, queue.Count);

        var second = queue.Single(e => e.ExecutionId == "e-a2");
        Assert.Equal(2, second.PositionInTenant);
        Assert.Equal(0, second.TenantsAheadInRotation);

        var otherTenant = queue.Single(e => e.ExecutionId == "e-b1");
        Assert.Equal(1, otherTenant.PositionInTenant);
        Assert.Equal(1, otherTenant.TenantsAheadInRotation);
        Assert.Equal(0, otherTenant.ExecutionClass);
        Assert.Equal("borrower-b", otherTenant.BorrowerTenantId);

        // The two numbers are independent: a position of 1 in one tenant and a position of 2 in
        // another do not order against each other, so no single number could stand in for the pair.
        Assert.False(queue.All(e => e.PositionInTenant == 0));
        Assert.Contains(queue, e => e.TenantsAheadInRotation > 0);
    }

    [Fact]
    public async Task GetQueue_LeasedEntry_CarriesItsMemberAndReportsItself_AsLeased()
    {
        _service.Respond(HttpStatusCode.OK, """
            [
              {"executionId":"e-run","borrowerTenantId":"borrower-a","pipelineRtId":"p1","pipelineName":"Nightly",
               "executionClass":1,"queuedAtUtc":"2026-09-14T07:59:00Z","positionInTenant":0,
               "tenantsAheadInRotation":0,"leasedOnMemberId":"member-3",
               "leaseExpiresAtUtc":"2026-09-14T08:10:00Z"}
            ]
            """);
        var client = CreateClient();

        var entry = Assert.Single(await client.GetAdapterPoolQueueAsync(AdapterPoolRtId));

        Assert.Equal("member-3", entry.LeasedOnMemberId);
        Assert.True(entry.IsLeased);
        Assert.Equal(0, entry.PositionInTenant);
        Assert.NotNull(entry.LeaseExpiresAtUtc);
    }

    [Fact]
    public async Task GetQueue_EmptyPool_IsAnEmptyListAndNotAFailure()
    {
        _service.Respond(HttpStatusCode.OK, "[]");
        var client = CreateClient();

        var queue = await client.GetAdapterPoolQueueAsync(AdapterPoolRtId);

        Assert.Empty(queue);
    }

    [Fact]
    public async Task CancelQueuedExecution_WaitingEntry_IsCancelled()
    {
        _service.Respond(HttpStatusCode.NoContent);
        var client = CreateClient();

        var result = await client.CancelQueuedExecutionAsync(AdapterPoolRtId, "e-a2");

        Assert.Equal(AdapterPoolQueueCancellationOutcome.Cancelled, result.Outcome);
        Assert.True(result.IsCancelled);
        Assert.Equal($"DELETE /lender/v1/adapterPool/{AdapterPoolRtId}/queue/e-a2", _service.SingleRequest());
    }

    [Fact]
    public async Task CancelQueuedExecution_AlreadyLeased_IsItsOwnOutcomeAndNotAnException()
    {
        _service.Respond(HttpStatusCode.Conflict,
            """{"errorMessage":"Execution 'e-run' already holds a lease and is no longer queued."}""");
        var client = CreateClient();

        var result = await client.CancelQueuedExecutionAsync(AdapterPoolRtId, "e-run");

        Assert.Equal(AdapterPoolQueueCancellationOutcome.AlreadyLeased, result.Outcome);
        Assert.False(result.IsCancelled);
        Assert.Contains("already holds a lease", result.ServerMessage);
    }

    [Fact]
    public async Task CancelQueuedExecution_UnknownEntry_IsNotFound()
    {
        _service.Respond(HttpStatusCode.NotFound,
            """{"errorMessage":"No queued execution 'e-gone' belongs to adapter pool."}""");
        var client = CreateClient();

        var result = await client.CancelQueuedExecutionAsync(AdapterPoolRtId, "e-gone");

        Assert.Equal(AdapterPoolQueueCancellationOutcome.NotFound, result.Outcome);
        Assert.Contains("e-gone", result.ServerMessage);
    }

    [Fact]
    public async Task CancelQueuedExecution_ServerError_StillThrows()
    {
        _service.Respond(HttpStatusCode.InternalServerError, """{"errorMessage":"boom"}""");
        var client = CreateClient();

        await Assert.ThrowsAsync<Meshmakers.Octo.Sdk.ServiceClient.ServiceClientResultException>(
            () => client.CancelQueuedExecutionAsync(AdapterPoolRtId, "e-a2"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task CancelQueuedExecution_BlankExecutionId_ThrowsBeforeSendingARequest(string? executionId)
    {
        var client = CreateClient();

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => client.CancelQueuedExecutionAsync(AdapterPoolRtId, executionId!));

        Assert.Equal("executionId", exception.ParamName);
        Assert.Empty(_service.Requests);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GetQueue_BlankPoolRtId_ThrowsBeforeSendingARequest(string? adapterPoolRtId)
    {
        var client = CreateClient();

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => client.GetAdapterPoolQueueAsync(adapterPoolRtId!));

        Assert.Equal("adapterPoolRtId", exception.ParamName);
        Assert.Empty(_service.Requests);
    }
}
