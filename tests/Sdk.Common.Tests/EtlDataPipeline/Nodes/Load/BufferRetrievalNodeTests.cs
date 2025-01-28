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

public class BufferRetrievalNodeTests(NodeFixture fixture) : IClassFixture<NodeFixture>
{
    private DataContext PrepareTest(BufferRetrievalNodeConfiguration r, object? data = null)
    {
        var logger = A.Fake<IPipelineLogger>();

        BuidDi(fixture.Services);
        
        var dataContext = new DataContext(
            fixture.Services.BuildServiceProvider(), logger)
        {
            Current = JObject.FromObject(data ?? new())
        };
        
        
        //reverse order
        dataContext.RegisterNode("BufferRetrievalNode", 0, r);
        
        return dataContext;
    }

    private void BuidDi(ServiceCollection fixtureServices)
    {
        fixtureServices.AddSingleton(typeof(IEdgeDataBuffer<>), typeof(EdgeDataBuffer<>));
        fixtureServices.AddSingleton<ILiteDBFactory, LiteDbFileFactory>();
    }

    [Fact]
    public async Task ProcessObjectAsync_DataNotModified_OK()
    {
        
        BufferNodeConfiguration b = new()
        {
            BufferTime = "00:00:10"
        };
        
        BufferRetrievalNodeConfiguration r = new()
        {
        };
        
        var dataContext = PrepareTest(r);

        var dataBuffer = dataContext.GlobalServiceProvider
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
        
        // 2. Fake the node context that returns our test configuration.
        var fakeNodeContext = A.Fake<INodeContext>();
        A.CallTo(() => fakeNodeContext.GetNodeConfiguration<BufferRetrievalNodeConfiguration>())
            .Returns(r);

        // 3. Fake the data context, which must return the fake node context.
        var fakeDataContext = A.Fake<IDataContext>();
        A.CallTo(() => fakeDataContext.NodeContext)
            .Returns(fakeNodeContext);

        var fn = A.Fake<NodeDelegate>();

        var retrievalNode = new BufferRetrievalNode(fn, dataBuffer);


        await retrievalNode.ProcessObjectAsync(dataContext);


        var array = dataContext.Current as JArray;

        
        Assert.NotNull(dataContext.Current);
        Assert.NotNull(array);
        Assert.Equal(5, array.Count);
    }

}