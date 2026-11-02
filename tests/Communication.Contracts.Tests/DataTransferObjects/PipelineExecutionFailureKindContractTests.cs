using System.Text.Json;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

namespace Communication.Contracts.Tests.DataTransferObjects;

/// <summary>
///     AB#8037 — the structured failure kind of a pipeline execution travels from the adapter to the controller on both
///     end reports (dedicated adapter: <see cref="PipelineExecutionEndDto" />, pool member: <see cref="LeaseResultDto" />).
///     Optional and defaulted, so both skew directions keep deserialising.
/// </summary>
public class PipelineExecutionFailureKindContractTests
{
    [Fact]
    public void EnumValues_MatchTheCkModelEnumKeys()
    {
        // System.Communication/PipelineExecutionFailureKind: 0 Failed, 1 OutcomeUnknown (keys are persisted)
        Assert.Equal(0, (int)PipelineExecutionFailureKind.Failed);
        Assert.Equal(1, (int)PipelineExecutionFailureKind.OutcomeUnknown);
        Assert.Equal(2, Enum.GetValues<PipelineExecutionFailureKind>().Length);
    }

    [Fact]
    public void EndReport_RoundTripsTheFailureKind()
    {
        var end = new PipelineExecutionEndDto
        {
            ExecutionId = Guid.NewGuid().ToString(),
            Status = PipelineExecutionStatus.Failed,
            CompletedAt = DateTime.UtcNow,
            DurationMs = 12,
            ErrorMessage = "write not acknowledged",
            FailureKind = PipelineExecutionFailureKind.OutcomeUnknown
        };

        var back = JsonSerializer.Deserialize<PipelineExecutionEndDto>(JsonSerializer.Serialize(end))!;

        Assert.Equal(PipelineExecutionFailureKind.OutcomeUnknown, back.FailureKind);
    }

    [Fact]
    public void EndReport_FromAnOlderAdapter_HasNoFailureKind()
    {
        var back = JsonSerializer.Deserialize<PipelineExecutionEndDto>(
            """{"ExecutionId":"e","Status":2,"CompletedAt":"2026-11-01T00:00:00Z","DurationMs":1,"ErrorMessage":"x"}""")!;

        Assert.Null(back.FailureKind);
    }

    [Fact]
    public void LeaseResult_RoundTripsTheFailureKind_AndAnOlderMemberSendsNone()
    {
        var result = new LeaseResultDto
        {
            LeaseId = "lease-1",
            Reason = LeaseReleaseReasonDto.Completed,
            Success = false,
            FailureKind = PipelineExecutionFailureKind.OutcomeUnknown
        };

        var back = JsonSerializer.Deserialize<LeaseResultDto>(JsonSerializer.Serialize(result))!;
        var older = JsonSerializer.Deserialize<LeaseResultDto>("""{"LeaseId":"lease-1","Reason":0,"Success":false}""")!;

        Assert.Equal(PipelineExecutionFailureKind.OutcomeUnknown, back.FailureKind);
        Assert.Null(older.FailureKind);
    }

    [Fact]
    public void LeaseResult_StillCarriesNoCredentialMaterial()
    {
        var properties = typeof(LeaseResultDto).GetProperties().Select(p => p.Name).ToList();

        Assert.Contains(nameof(LeaseResultDto.FailureKind), properties);
        Assert.DoesNotContain("ClientSecret", properties);
    }
}

/// <summary>
///     AB#8037 — <see cref="PipelineExecutionFailureKinds.Of" /> delegates to the engine classifier. Driver-free here: a
///     type in a <c>MongoDB.Driver</c> namespace with a <c>WriteConcernError</c> stands in for the driver exception (the
///     real driver exceptions are covered by the engine's <c>UnacknowledgedWriteClassifierTests</c>).
/// </summary>
public class PipelineExecutionFailureKindsTests
{
    [Fact]
    public void AnUnacknowledgedWrite_IsOutcomeUnknown_AlsoWrapped()
    {
        var fault = new MongoDB.Driver.FakeWriteConcernFaultException();

        Assert.Equal(PipelineExecutionFailureKind.OutcomeUnknown, PipelineExecutionFailureKinds.Of(fault));
        Assert.Equal(PipelineExecutionFailureKind.OutcomeUnknown,
            PipelineExecutionFailureKinds.Of(new AggregateException(new InvalidOperationException("wrap", fault))));
    }

    [Fact]
    public void AnyOtherException_OrNone_IsFailed()
    {
        Assert.Equal(PipelineExecutionFailureKind.Failed,
            PipelineExecutionFailureKinds.Of(new InvalidOperationException("db is down")));
        Assert.Equal(PipelineExecutionFailureKind.Failed, PipelineExecutionFailureKinds.Of(null));
    }
}
