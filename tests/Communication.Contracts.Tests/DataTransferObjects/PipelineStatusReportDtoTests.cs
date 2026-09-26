using System.Text.Json;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts;

namespace Communication.Contracts.Tests.DataTransferObjects;

/// <summary>
/// AB#5385: the status line a trigger node reports after every poll travels over the adapter hub
/// as <see cref="PipelineStatusReportDto"/>. Pins the wire shape — the controller reads the same four
/// members and an accidentally renamed one would silently arrive as a default.
/// </summary>
public class PipelineStatusReportDtoTests
{
    [Fact]
    public void PipelineStatusReportDto_RoundTripsThroughJson()
    {
        // Arrange
        var pipelineRtEntityId = new RtEntityId("System.Communication/Pipeline", OctoObjectId.GenerateNewId());
        var timestamp = new DateTime(2026, 9, 26, 17, 40, 12, DateTimeKind.Utc);
        var dto = new PipelineStatusReportDto
        {
            PipelineRtEntityId = pipelineRtEntityId,
            Message = "2026-09-26T17:40:12Z · kbernkopf@tecob.at · Inbox/Eingangsrechnungen · seen 12, imported 12",
            IsError = false,
            TimestampUtc = timestamp
        };

        // Act
        var json = JsonSerializer.Serialize(dto);
        var round = JsonSerializer.Deserialize<PipelineStatusReportDto>(json);

        // Assert
        Assert.NotNull(round);
        Assert.Equal(pipelineRtEntityId, round!.PipelineRtEntityId);
        Assert.Equal(dto.Message, round.Message);
        Assert.False(round.IsError);
        Assert.Equal(timestamp, round.TimestampUtc);
    }

    [Fact]
    public void PipelineStatusReportDto_ErrorFlagSurvivesRoundTrip()
    {
        var dto = new PipelineStatusReportDto
        {
            PipelineRtEntityId = new RtEntityId("System.Communication/Pipeline", OctoObjectId.GenerateNewId()),
            Message = "ERROR 2026-09-26T17:40:12Z · Mail folder 'Inbox.02_Steuern' not found",
            IsError = true,
            TimestampUtc = DateTime.UtcNow
        };

        var round = JsonSerializer.Deserialize<PipelineStatusReportDto>(JsonSerializer.Serialize(dto));

        Assert.NotNull(round);
        Assert.True(round!.IsError);
        Assert.StartsWith("ERROR ", round.Message);
    }
}
