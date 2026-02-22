using FakeItEasy;
using LiteDB;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Buffering;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Buffering.EdgeBuffer;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Sdk.Common.Tests.Fixtures;

namespace Sdk.Common.Tests.EtlDataPipeline.Nodes.Load;

public class BufferNodeTests(NodeFixture fixture) : IClassFixture<NodeFixture>
{
    [Fact]
    public async Task ProcessObjectAsync_StoresDataInBuffer_CallsNext()
    {
        // Arrange
        var chunk = A.Fake<IChunkedDataBuffer<Dictionary<string, BsonValue>>>();
        var buffer = A.Fake<IEdgeDataBuffer<Dictionary<string, BsonValue>>>();
        A.CallTo(() => buffer.GetOrCreateOpenChunk()).Returns(chunk);

        var etlContext = CreateEtlContext();
        var orchestrator = A.Fake<IEtlDataOrchestrator>();
        var scheduler = A.Fake<IBufferScheduler>();
        fixture.Services.AddSingleton(scheduler);

        var config = new BufferNodeConfiguration
        {
            Path = "$",
            BufferTime = "00:05:00"
        };

        var testData = new JObject
        {
            ["temperature"] = 42.5,
            ["sensor"] = "T1"
        };
        var (dataContext, nodeContext) = PrepareTest(config, testData);

        var fn = A.Fake<NodeDelegate>();
        var testee = new BufferNode(fn, buffer, etlContext, orchestrator);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => buffer.GetOrCreateOpenChunk()).MustHaveHappenedOnceExactly();
        A.CallTo(() => chunk.AddDataPoint(A<DataPoint<Dictionary<string, BsonValue>>>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ProcessObjectAsync_ClearsCurrentAfterBuffering()
    {
        // Arrange
        var chunk = A.Fake<IChunkedDataBuffer<Dictionary<string, BsonValue>>>();
        var buffer = A.Fake<IEdgeDataBuffer<Dictionary<string, BsonValue>>>();
        A.CallTo(() => buffer.GetOrCreateOpenChunk()).Returns(chunk);

        var etlContext = CreateEtlContext();
        var orchestrator = A.Fake<IEtlDataOrchestrator>();
        var scheduler = A.Fake<IBufferScheduler>();
        fixture.Services.AddSingleton(scheduler);

        var config = new BufferNodeConfiguration
        {
            Path = "$",
            BufferTime = "00:01:00"
        };

        var testData = new JObject { ["value"] = 100 };
        var (dataContext, nodeContext) = PrepareTest(config, testData);

        var fn = A.Fake<NodeDelegate>();
        var testee = new BufferNode(fn, buffer, etlContext, orchestrator);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert - current should be an empty JObject after buffering
        Assert.NotNull(dataContext.Current);
        Assert.IsType<JObject>(dataContext.Current);
        Assert.Empty((JObject)dataContext.Current);
    }

    [Fact]
    public async Task ProcessObjectAsync_SchedulesBufferFlush_WhenFirstRun()
    {
        // Arrange
        var chunk = A.Fake<IChunkedDataBuffer<Dictionary<string, BsonValue>>>();
        var buffer = A.Fake<IEdgeDataBuffer<Dictionary<string, BsonValue>>>();
        A.CallTo(() => buffer.GetOrCreateOpenChunk()).Returns(chunk);

        var etlContext = CreateEtlContext();
        var orchestrator = A.Fake<IEtlDataOrchestrator>();
        var scheduler = A.Fake<IBufferScheduler>();

        fixture.Services.AddSingleton(scheduler);

        var config = new BufferNodeConfiguration
        {
            Path = "$",
            BufferTime = "00:10:00"
        };

        var (dataContext, nodeContext) = PrepareTest(config, new JObject { ["v"] = 1 });

        var fn = A.Fake<NodeDelegate>();
        var testee = new BufferNode(fn, buffer, etlContext, orchestrator);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => scheduler.ScheduleOrReplace(A<Func<Task>>._, TimeSpan.FromMinutes(10)))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ProcessObjectAsync_WithInvalidBufferTime_DefaultsTo10Seconds()
    {
        // Arrange
        var chunk = A.Fake<IChunkedDataBuffer<Dictionary<string, BsonValue>>>();
        var buffer = A.Fake<IEdgeDataBuffer<Dictionary<string, BsonValue>>>();
        A.CallTo(() => buffer.GetOrCreateOpenChunk()).Returns(chunk);

        var etlContext = CreateEtlContext();
        var orchestrator = A.Fake<IEtlDataOrchestrator>();
        var scheduler = A.Fake<IBufferScheduler>();

        fixture.Services.AddSingleton(scheduler);

        var config = new BufferNodeConfiguration
        {
            Path = "$",
            BufferTime = "invalid-time"
        };

        var (dataContext, nodeContext) = PrepareTest(config, new JObject { ["v"] = 1 });

        var fn = A.Fake<NodeDelegate>();
        var testee = new BufferNode(fn, buffer, etlContext, orchestrator);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert - falls back to 10 seconds
        A.CallTo(() => scheduler.ScheduleOrReplace(A<Func<Task>>._, TimeSpan.FromSeconds(10)))
            .MustHaveHappenedOnceExactly();
    }

    private (DataContext, INodeContext) PrepareTest(BufferNodeConfiguration config, JToken data)
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = data.DeepClone()
        };

        var rootNodeContext =
            NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("BufferData", 0, config, dataContext);

        return (dataContext, nodeContext);
    }

    private static IEtlContext CreateEtlContext()
    {
        var etlContext = A.Fake<IEtlContext>();
        A.CallTo(() => etlContext.Properties).Returns(new Dictionary<string, object?>());
        return etlContext;
    }
}
