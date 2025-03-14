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
    private (DataContext, INodeContext) PrepareDataContext(object? data = null)
    {
        BufferRetrievalNodeConfiguration r = new();
        var logger = A.Fake<IPipelineLogger>();

        BuidDi(fixture.Services);
        
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(data ?? new())
        };
        
        
        //reverse order
        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("BufferRetrievalNode", 0, r, dataContext);
        
        return (dataContext, nodeContext);
    }

    private void BuidDi(ServiceCollection fixtureServices)
    {
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
}