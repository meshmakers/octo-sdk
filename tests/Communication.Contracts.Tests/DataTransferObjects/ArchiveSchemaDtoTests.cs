using System.Text.Json;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

namespace Communication.Contracts.Tests.DataTransferObjects;

public class ArchiveSchemaDtoTests
{
    [Fact]
    public void ArchiveSchemaDto_RawArchive_RoundTripsThroughJson()
    {
        // Arrange — mirrors the metadata.archive block for a raw archive (concept §3.1).
        var schema = new ArchiveSchemaDto(
            RtId: "665f0000000000000000ee21",
            RtWellKnownName: "voltage-raw",
            Kind: "raw",
            TargetCkTypeId: "Sensor",
            Columns: new[]
            {
                new ArchiveColumnDto("voltage", true, false),
                new ArchiveColumnDto("current", false, false)
            },
            RollupAggregations: null,
            Period: null);

        // Act
        var json = JsonSerializer.Serialize(schema);
        var round = JsonSerializer.Deserialize<ArchiveSchemaDto>(json);

        // Assert
        Assert.NotNull(round);
        Assert.Equal(schema.RtId, round!.RtId);
        Assert.Equal(schema.RtWellKnownName, round.RtWellKnownName);
        Assert.Equal(schema.TargetCkTypeId, round.TargetCkTypeId);
        Assert.Equal("raw", round.Kind);
        Assert.Equal(2, round.Columns.Count);
        Assert.Equal("voltage", round.Columns[0].Path);
        Assert.True(round.Columns[0].Indexed);
        Assert.Null(round.RollupAggregations);
        Assert.Null(round.Period);
    }

    [Fact]
    public void ArchiveSchemaDto_RollupArchive_CarriesAggregations()
    {
        // Arrange
        var schema = new ArchiveSchemaDto(
            RtId: "665f0000000000000000ee22",
            RtWellKnownName: null,
            Kind: "rollup",
            TargetCkTypeId: "Sensor",
            Columns: new[] { new ArchiveColumnDto("temperature_avg", false, false) },
            RollupAggregations: new[] { new ArchiveRollupAggregationDto("temperature", "avg", "temperature_avg") },
            Period: null);

        // Act
        var json = JsonSerializer.Serialize(schema);
        var round = JsonSerializer.Deserialize<ArchiveSchemaDto>(json);

        // Assert
        Assert.NotNull(round);
        Assert.Equal("rollup", round!.Kind);
        Assert.NotNull(round.RollupAggregations);
        Assert.Single(round.RollupAggregations!);
        Assert.Equal("temperature", round.RollupAggregations![0].SourcePath);
        Assert.Equal("avg", round.RollupAggregations![0].Function);
        Assert.Equal("temperature_avg", round.RollupAggregations![0].TargetColumnName);
    }

    [Fact]
    public void ArchiveSchemaDto_TimeRangeArchive_CarriesPeriod()
    {
        // Arrange
        var schema = new ArchiveSchemaDto(
            RtId: "665f0000000000000000ee23",
            RtWellKnownName: "windowed",
            Kind: "timeRange",
            TargetCkTypeId: "Sensor",
            Columns: Array.Empty<ArchiveColumnDto>(),
            RollupAggregations: null,
            Period: TimeSpan.FromMinutes(15));

        // Act
        var json = JsonSerializer.Serialize(schema);
        var round = JsonSerializer.Deserialize<ArchiveSchemaDto>(json);

        // Assert
        Assert.NotNull(round);
        Assert.Equal(TimeSpan.FromMinutes(15), round!.Period);
        Assert.Equal("timeRange", round.Kind);
    }

    [Theory]
    [InlineData(ArchiveImportMode.InsertOnly, "InsertOnly")]
    [InlineData(ArchiveImportMode.Upsert, "Upsert")]
    public void ArchiveImportMode_ToString_MatchesWireQueryValue(ArchiveImportMode mode, string expected)
    {
        // The clients serialize the mode as its name onto the query string (?mode=Upsert).
        Assert.Equal(expected, mode.ToString());
    }
}
