using FakeItEasy;
using LiteDB;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Buffering;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Buffering.EdgeBuffer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Sdk.Common.Tests.Fixtures;

namespace Sdk.Common.Tests.EtlDataPipeline.Nodes.Load;

public class BufferRetrievalNodeTests : IDisposable
{
    private string? _tempStoragePath;

    private (DataContext, INodeContext) PrepareDataContext(object? data = null)
    {
        BufferRetrievalNodeConfiguration r = new();
        var logger = A.Fake<IPipelineLogger>();

        // Pre-#3519 this used the shared NodeFixture.Services and modified it
        // in place. That left two layers of races behind: parallel test runs
        // overwrote each other's `EdgeDataBufferConfiguration.StoragePath`,
        // and repeated `AddSingleton(IEdgeDataBuffer<>, …)` calls accumulated
        // duplicate registrations across test instances. Both manifested as
        // BufferRetrievalNode reading from someone else's LiteDB file (or no
        // file at all → empty array → the AssertNotNull(array) failure seen
        // in CI). Per-test ServiceCollection removes both classes of flake.
        var services = new ServiceCollection();
        BuidDi(services);

        var dataContext = new DataContext
        {
            Current = JObject.FromObject(data ?? new())
        };


        //reverse order
        var rootNodeContext = NodeContext.CreateRootNodeContext(services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("BufferRetrievalNode", 0, r, dataContext);

        return (dataContext, nodeContext);
    }

    private void BuidDi(ServiceCollection fixtureServices)
    {
        _tempStoragePath = Path.Combine(Path.GetTempPath(), "BufferRetrievalNodeTests_" + Guid.NewGuid().ToString("N"));
        // EdgeDataBuffer<> resolves ILoggerFactory; the old shared NodeFixture
        // inherited that registration from ServiceCollectionFixture. With per-test
        // services we need to recreate it here.
        fixtureServices.AddLogging(b => b.ClearProviders().SetMinimumLevel(LogLevel.Trace));
        fixtureServices.AddOptions();
        fixtureServices.Configure<EdgeDataBufferConfiguration>(options =>
        {
            options.StoragePath = _tempStoragePath;
        });
        fixtureServices.AddSingleton(typeof(IEdgeDataBuffer<>), typeof(EdgeDataBuffer<>));
        fixtureServices.AddSingleton<ILiteDBFactory, LiteDbFileFactory>();
    }

    [Fact]
    public async Task ProcessObjectAsync_DataNotModified_OK()
    {
        var (dataContext, nodeContext) = PrepareDataContext();
        var dataBuffer = InsertTestData(nodeContext);

        var fn = A.Fake<NodeDelegate>();
        var retrievalNode = new BufferRetrievalNode(fn, dataBuffer);


        await retrievalNode.ProcessObjectAsync(dataContext, nodeContext);


        var array = dataContext.Current as JArray;

        Assert.NotNull(dataContext.Current);
        Assert.NotNull(array);
        Assert.Equal(5, array.Count);

        var firstObject = array[0] as JObject;

        Assert.NotNull(firstObject);
        Assert.Equal(1, firstObject["a"]);
        Assert.Equal(1, firstObject["b"]);
        Assert.Equal(1, firstObject["c"]);
    }

    private static IEdgeDataBuffer<Dictionary<string, BsonValue>> InsertTestData(INodeContext nodeContext)
    {
        var dataBuffer = nodeContext.ServiceProvider
            .GetRequiredService<IEdgeDataBuffer<Dictionary<string, BsonValue>>>();


        var chunk = dataBuffer.GetOrCreateOpenChunk();
        chunk.AddDataPoint(DataPoint<Dictionary<string, BsonValue>>.CreateNew(new Dictionary<string, BsonValue>()
        {
            ["a"] = 1,
            ["b"] = 1,
            ["c"] = 1
        }));
        chunk.AddDataPoint(DataPoint<Dictionary<string, BsonValue>>.CreateNew(new Dictionary<string, BsonValue>()
        {
            ["a"] = 2,
            ["c"] = 2
        }));
        chunk.AddDataPoint(DataPoint<Dictionary<string, BsonValue>>.CreateNew(new Dictionary<string, BsonValue>()
        {
            ["a"] = 3,
            ["b"] = 3,
        }));
        chunk.AddDataPoint(DataPoint<Dictionary<string, BsonValue>>.CreateNew(new Dictionary<string, BsonValue>()
        {
            ["b"] = 4,
            ["c"] = 4,
        }));
        chunk.AddDataPoint(DataPoint<Dictionary<string, BsonValue>>.CreateNew(new Dictionary<string, BsonValue>()
        {
            ["x"] = 5,
            ["y"] = 5,
            ["z"] = 5
        }));

        dataBuffer.TryCloseCurrentChunk(true);
        return dataBuffer;
    }

    public void Dispose()
    {
        if (_tempStoragePath != null && Directory.Exists(_tempStoragePath))
        {
            try
            {
                Directory.Delete(_tempStoragePath, true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }
}
