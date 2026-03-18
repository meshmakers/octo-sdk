#pragma warning disable CS8602 // Dereference of a possibly null reference.

using FakeItEasy;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;
using Meshmakers.Octo.Sdk.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Sdk.Common.Tests.Fixtures;

namespace Sdk.Common.Tests.EtlDataPipeline.Nodes.Transforms;

public class DateTimeNodeTests(NodeFixture fixture) : IClassFixture<NodeFixture>
{
    private static readonly DateTime ReferenceDate = new(2025, 6, 15, 14, 30, 45, DateTimeKind.Utc);
    private static readonly DateTime OtherDate = new(2025, 6, 20, 8, 15, 0, DateTimeKind.Utc);

    private (DataContext, INodeContext) PrepareTest(DateTimeNodeConfiguration config, object? testData = null)
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(testData ?? new
            {
                timestamp = ReferenceDate,
                otherTimestamp = OtherDate,
                daysToAdd = 5.0,
                hoursToAdd = 3.5,
                minutesToAdd = 90.0,
                secondsToAdd = 120.0,
            })
        };
        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("DateTime", 0, config, dataContext);
        return (dataContext, nodeContext);
    }

    [Fact]
    public async Task ProcessObjectAsync_Now_ReturnsCurrentUtcDateTime()
    {
        // Arrange
        var config = new DateTimeNodeConfiguration
        {
            Operation = DateTimeOperationDto.Now,
            TargetPath = "$.result",
        };

        var (dataContext, nodeContext) = PrepareTest(config);
        var fn = A.Fake<NodeDelegate>();
        var testee = new DateTimeNode(fn);
        var before = DateTime.UtcNow;

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        var after = DateTime.UtcNow;
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        var result = dataContext.Current["result"]!.ToObject<DateTime>();
        Assert.True(result >= before && result <= after);
    }

    [Fact]
    public async Task ProcessObjectAsync_StartOfDay_TruncatesToMidnight()
    {
        var config = new DateTimeNodeConfiguration
        {
            Operation = DateTimeOperationDto.StartOfDay,
            Path = "$.timestamp",
            TargetPath = "$.result",
        };

        var (dataContext, nodeContext) = PrepareTest(config);
        var fn = A.Fake<NodeDelegate>();
        var testee = new DateTimeNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        var result = dataContext.Current["result"]!.ToObject<DateTime>();
        Assert.Equal(new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public async Task ProcessObjectAsync_AddDays_WithValue_OK()
    {
        var config = new DateTimeNodeConfiguration
        {
            Operation = DateTimeOperationDto.AddDays,
            Path = "$.timestamp",
            Value = 3.0,
            TargetPath = "$.result",
        };

        var (dataContext, nodeContext) = PrepareTest(config);
        var fn = A.Fake<NodeDelegate>();
        var testee = new DateTimeNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        var result = dataContext.Current["result"]!.ToObject<DateTime>();
        Assert.Equal(ReferenceDate.AddDays(3), result);
    }

    [Fact]
    public async Task ProcessObjectAsync_AddDays_WithValuePath_OK()
    {
        var config = new DateTimeNodeConfiguration
        {
            Operation = DateTimeOperationDto.AddDays,
            Path = "$.timestamp",
            ValuePath = "$.daysToAdd",
            TargetPath = "$.result",
        };

        var (dataContext, nodeContext) = PrepareTest(config);
        var fn = A.Fake<NodeDelegate>();
        var testee = new DateTimeNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        var result = dataContext.Current["result"]!.ToObject<DateTime>();
        Assert.Equal(ReferenceDate.AddDays(5), result);
    }

    [Fact]
    public async Task ProcessObjectAsync_AddDays_NegativeValue_OK()
    {
        var config = new DateTimeNodeConfiguration
        {
            Operation = DateTimeOperationDto.AddDays,
            Path = "$.timestamp",
            Value = -2.0,
            TargetPath = "$.result",
        };

        var (dataContext, nodeContext) = PrepareTest(config);
        var fn = A.Fake<NodeDelegate>();
        var testee = new DateTimeNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        var result = dataContext.Current["result"]!.ToObject<DateTime>();
        Assert.Equal(ReferenceDate.AddDays(-2), result);
    }

    [Fact]
    public async Task ProcessObjectAsync_AddHours_WithValue_OK()
    {
        var config = new DateTimeNodeConfiguration
        {
            Operation = DateTimeOperationDto.AddHours,
            Path = "$.timestamp",
            Value = 3.5,
            TargetPath = "$.result",
        };

        var (dataContext, nodeContext) = PrepareTest(config);
        var fn = A.Fake<NodeDelegate>();
        var testee = new DateTimeNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        var result = dataContext.Current["result"]!.ToObject<DateTime>();
        Assert.Equal(ReferenceDate.AddHours(3.5), result);
    }

    [Fact]
    public async Task ProcessObjectAsync_AddHours_WithValuePath_OK()
    {
        var config = new DateTimeNodeConfiguration
        {
            Operation = DateTimeOperationDto.AddHours,
            Path = "$.timestamp",
            ValuePath = "$.hoursToAdd",
            TargetPath = "$.result",
        };

        var (dataContext, nodeContext) = PrepareTest(config);
        var fn = A.Fake<NodeDelegate>();
        var testee = new DateTimeNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        var result = dataContext.Current["result"]!.ToObject<DateTime>();
        Assert.Equal(ReferenceDate.AddHours(3.5), result);
    }

    [Fact]
    public async Task ProcessObjectAsync_AddMinutes_WithValue_OK()
    {
        var config = new DateTimeNodeConfiguration
        {
            Operation = DateTimeOperationDto.AddMinutes,
            Path = "$.timestamp",
            Value = 90.0,
            TargetPath = "$.result",
        };

        var (dataContext, nodeContext) = PrepareTest(config);
        var fn = A.Fake<NodeDelegate>();
        var testee = new DateTimeNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        var result = dataContext.Current["result"]!.ToObject<DateTime>();
        Assert.Equal(ReferenceDate.AddMinutes(90), result);
    }

    [Fact]
    public async Task ProcessObjectAsync_AddSeconds_WithValue_OK()
    {
        var config = new DateTimeNodeConfiguration
        {
            Operation = DateTimeOperationDto.AddSeconds,
            Path = "$.timestamp",
            Value = 120.0,
            TargetPath = "$.result",
        };

        var (dataContext, nodeContext) = PrepareTest(config);
        var fn = A.Fake<NodeDelegate>();
        var testee = new DateTimeNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        var result = dataContext.Current["result"]!.ToObject<DateTime>();
        Assert.Equal(ReferenceDate.AddSeconds(120), result);
    }

    [Fact]
    public async Task ProcessObjectAsync_DaysBetween_OK()
    {
        var config = new DateTimeNodeConfiguration
        {
            Operation = DateTimeOperationDto.DaysBetween,
            Path = "$.timestamp",
            ValuePath = "$.otherTimestamp",
            TargetPath = "$.result",
        };

        var (dataContext, nodeContext) = PrepareTest(config);
        var fn = A.Fake<NodeDelegate>();
        var testee = new DateTimeNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        // OtherDate (Jun 20) - ReferenceDate (Jun 15) = 5 days
        var result = dataContext.Current["result"]!.ToObject<int>();
        Assert.Equal(5, result);
    }

    [Fact]
    public async Task ProcessObjectAsync_DaysBetween_NegativeResult_OK()
    {
        var config = new DateTimeNodeConfiguration
        {
            Operation = DateTimeOperationDto.DaysBetween,
            Path = "$.otherTimestamp",
            ValuePath = "$.timestamp",
            TargetPath = "$.result",
        };

        var (dataContext, nodeContext) = PrepareTest(config);
        var fn = A.Fake<NodeDelegate>();
        var testee = new DateTimeNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        // ReferenceDate (Jun 15) - OtherDate (Jun 20) = -5 days
        var result = dataContext.Current["result"]!.ToObject<int>();
        Assert.Equal(-5, result);
    }

    [Fact]
    public async Task ProcessObjectAsync_DaysBetween_IgnoresTimePart()
    {
        // Two dates on the same day but different times → 0 days
        var config = new DateTimeNodeConfiguration
        {
            Operation = DateTimeOperationDto.DaysBetween,
            Path = "$.morning",
            ValuePath = "$.evening",
            TargetPath = "$.result",
        };

        var testData = new
        {
            morning = new DateTime(2025, 3, 15, 6, 0, 0, DateTimeKind.Utc),
            evening = new DateTime(2025, 3, 15, 22, 0, 0, DateTimeKind.Utc),
        };

        var (dataContext, nodeContext) = PrepareTest(config, testData);
        var fn = A.Fake<NodeDelegate>();
        var testee = new DateTimeNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(0, dataContext.Current["result"]!.ToObject<int>());
    }

    [Fact]
    public async Task ProcessObjectAsync_Format_OK()
    {
        var config = new DateTimeNodeConfiguration
        {
            Operation = DateTimeOperationDto.Format,
            Path = "$.timestamp",
            Value = "yyyy-MM-ddTHH:mm",
            TargetPath = "$.result",
        };

        var (dataContext, nodeContext) = PrepareTest(config);
        var fn = A.Fake<NodeDelegate>();
        var testee = new DateTimeNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("2025-06-15T14:30", dataContext.Current["result"]!.ToObject<string>());
    }

    [Fact]
    public async Task ProcessObjectAsync_Format_DateOnly_OK()
    {
        var config = new DateTimeNodeConfiguration
        {
            Operation = DateTimeOperationDto.Format,
            Path = "$.timestamp",
            Value = "yyyy-MM-dd",
            TargetPath = "$.result",
        };

        var (dataContext, nodeContext) = PrepareTest(config);
        var fn = A.Fake<NodeDelegate>();
        var testee = new DateTimeNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("2025-06-15", dataContext.Current["result"]!.ToObject<string>());
    }

    [Fact]
    public async Task ProcessObjectAsync_CombineDateTime_OK()
    {
        var config = new DateTimeNodeConfiguration
        {
            Operation = DateTimeOperationDto.CombineDateTime,
            Path = "$.dateSource",
            ValuePath = "$.timeSource",
            TargetPath = "$.result",
        };

        var testData = new
        {
            dateSource = new DateTime(2025, 3, 15, 0, 0, 0, DateTimeKind.Utc),
            timeSource = new DateTime(2025, 1, 1, 14, 30, 0, DateTimeKind.Utc),
        };

        var (dataContext, nodeContext) = PrepareTest(config, testData);
        var fn = A.Fake<NodeDelegate>();
        var testee = new DateTimeNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        var result = dataContext.Current["result"]!.ToObject<DateTime>();
        Assert.Equal(new DateTime(2025, 3, 15, 14, 30, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public async Task ProcessObjectAsync_ExtractDate_TruncatesToMidnight()
    {
        var config = new DateTimeNodeConfiguration
        {
            Operation = DateTimeOperationDto.ExtractDate,
            Path = "$.timestamp",
            TargetPath = "$.result",
        };

        var (dataContext, nodeContext) = PrepareTest(config);
        var fn = A.Fake<NodeDelegate>();
        var testee = new DateTimeNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        var result = dataContext.Current["result"]!.ToObject<DateTime>();
        Assert.Equal(new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public async Task ProcessObjectAsync_ExtractTime_ReturnsTimeString()
    {
        var config = new DateTimeNodeConfiguration
        {
            Operation = DateTimeOperationDto.ExtractTime,
            Path = "$.timestamp",
            TargetPath = "$.result",
        };

        var (dataContext, nodeContext) = PrepareTest(config);
        var fn = A.Fake<NodeDelegate>();
        var testee = new DateTimeNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("14:30:45", dataContext.Current["result"]!.ToObject<string>());
    }

    [Fact]
    public async Task ProcessObjectAsync_NullInputValue_ThrowsException()
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext { Current = null };
        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);

        var config = new DateTimeNodeConfiguration
        {
            Operation = DateTimeOperationDto.Now,
            TargetPath = "$.result",
        };

        var nodeContext = rootNodeContext.RegisterChildNode("DateTime", 0, config, dataContext);
        var fn = A.Fake<NodeDelegate>();
        var testee = new DateTimeNode(fn);

        await Assert.ThrowsAsync<PipelineExecutionException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_MissingSourcePath_ThrowsException()
    {
        var config = new DateTimeNodeConfiguration
        {
            Operation = DateTimeOperationDto.StartOfDay,
            Path = "$.nonexistent",
            TargetPath = "$.result",
        };

        var (dataContext, nodeContext) = PrepareTest(config);
        var fn = A.Fake<NodeDelegate>();
        var testee = new DateTimeNode(fn);

        await Assert.ThrowsAsync<PipelineExecutionException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_AddDays_MissingValue_ThrowsException()
    {
        var config = new DateTimeNodeConfiguration
        {
            Operation = DateTimeOperationDto.AddDays,
            Path = "$.timestamp",
            TargetPath = "$.result",
            // No Value or ValuePath set
        };

        var (dataContext, nodeContext) = PrepareTest(config);
        var fn = A.Fake<NodeDelegate>();
        var testee = new DateTimeNode(fn);

        await Assert.ThrowsAsync<PipelineExecutionException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_Format_MissingFormatString_ThrowsException()
    {
        var config = new DateTimeNodeConfiguration
        {
            Operation = DateTimeOperationDto.Format,
            Path = "$.timestamp",
            TargetPath = "$.result",
            // No Value set for format string
        };

        var (dataContext, nodeContext) = PrepareTest(config);
        var fn = A.Fake<NodeDelegate>();
        var testee = new DateTimeNode(fn);

        await Assert.ThrowsAsync<PipelineExecutionException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_UnsupportedOperation_ThrowsException()
    {
        var config = new DateTimeNodeConfiguration
        {
            Operation = (DateTimeOperationDto)999,
            Path = "$.timestamp",
            TargetPath = "$.result",
        };

        var (dataContext, nodeContext) = PrepareTest(config);
        var fn = A.Fake<NodeDelegate>();
        var testee = new DateTimeNode(fn);

        await Assert.ThrowsAsync<NotSupportedException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_DaysBetween_MissingValuePath_ThrowsException()
    {
        var config = new DateTimeNodeConfiguration
        {
            Operation = DateTimeOperationDto.DaysBetween,
            Path = "$.timestamp",
            TargetPath = "$.result",
            // No ValuePath set
        };

        var (dataContext, nodeContext) = PrepareTest(config);
        var fn = A.Fake<NodeDelegate>();
        var testee = new DateTimeNode(fn);

        await Assert.ThrowsAsync<PipelineExecutionException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_AddDays_ValuePathTakesPrecedenceOverValue()
    {
        var config = new DateTimeNodeConfiguration
        {
            Operation = DateTimeOperationDto.AddDays,
            Path = "$.timestamp",
            Value = 100.0, // This should be ignored
            ValuePath = "$.daysToAdd", // 5.0 from test data
            TargetPath = "$.result",
        };

        var (dataContext, nodeContext) = PrepareTest(config);
        var fn = A.Fake<NodeDelegate>();
        var testee = new DateTimeNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        var result = dataContext.Current["result"]!.ToObject<DateTime>();
        Assert.Equal(ReferenceDate.AddDays(5), result); // Uses ValuePath (5), not Value (100)
    }

    [Fact]
    public async Task ProcessObjectAsync_MidnightBoundary_OK()
    {
        var config = new DateTimeNodeConfiguration
        {
            Operation = DateTimeOperationDto.StartOfDay,
            Path = "$.midnight",
            TargetPath = "$.result",
        };

        var testData = new
        {
            midnight = new DateTime(2025, 3, 15, 0, 0, 0, DateTimeKind.Utc),
        };

        var (dataContext, nodeContext) = PrepareTest(config, testData);
        var fn = A.Fake<NodeDelegate>();
        var testee = new DateTimeNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        var result = dataContext.Current["result"]!.ToObject<DateTime>();
        Assert.Equal(new DateTime(2025, 3, 15, 0, 0, 0, DateTimeKind.Utc), result);
    }
}
