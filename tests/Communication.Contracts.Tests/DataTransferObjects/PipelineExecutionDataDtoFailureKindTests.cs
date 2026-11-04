using System.Text.Json;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

namespace Communication.Contracts.Tests.DataTransferObjects;

/// <summary>
///     AB#8156 — the REST pipeline execution (<see cref="PipelineExecutionDataDto" />) carries the failure kind of
///     AB#8037 so an operator sees whether a failed execution was an unacknowledged write. The field is additive:
///     null when absent, and an older client ignores it.
/// </summary>
public class PipelineExecutionDataDtoFailureKindTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public void FailureKind_IsSerializedAsTheEnumName()
    {
        var dto = new PipelineExecutionDataDto
        {
            Id = Guid.NewGuid(),
            DateTime = DateTime.UtcNow,
            Status = PipelineExecutionStatus.Failed,
            FailureKind = PipelineExecutionFailureKind.OutcomeUnknown
        };

        var json = JsonSerializer.Serialize(dto, Web);

        Assert.Contains("\"failureKind\":\"OutcomeUnknown\"", json);
        Assert.Equal(PipelineExecutionFailureKind.OutcomeUnknown,
            JsonSerializer.Deserialize<PipelineExecutionDataDto>(json, Web)!.FailureKind);
    }

    [Fact]
    public void FailureKind_FromAnOlderController_IsNull()
    {
        var back = JsonSerializer.Deserialize<PipelineExecutionDataDto>(
            """{"id":"6f9619ff-8b86-d011-b42d-00c04fc964ff","dateTime":"2026-11-01T00:00:00Z","status":"Failed","errorMessage":"x"}""",
            Web)!;

        Assert.Null(back.FailureKind);
    }

    [Fact]
    public void FailureKind_Failed_RoundTrips()
    {
        var back = JsonSerializer.Deserialize<PipelineExecutionDataDto>(
            """{"id":"6f9619ff-8b86-d011-b42d-00c04fc964ff","dateTime":"2026-11-01T00:00:00Z","status":"Failed","failureKind":"Failed"}""",
            Web)!;

        Assert.Equal(PipelineExecutionFailureKind.Failed, back.FailureKind);
    }
}
