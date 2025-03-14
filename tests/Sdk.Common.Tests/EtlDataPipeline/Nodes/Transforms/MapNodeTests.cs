using FakeItEasy;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Sdk.Common.Tests.Fixtures;
using Sdk.Common.Tests.TestData.Dto;

namespace Sdk.Common.Tests.EtlDataPipeline.Nodes.Transforms;

public class MapNodeTests(NodeFixture fixture) : IClassFixture<NodeFixture>
{
    private (DataContext, INodeContext) PrepareTest(MapNodeConfiguration mapNodeConfiguration)
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(Generator.GenerateColumnData())
        };
        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Map", 0, mapNodeConfiguration, dataContext);
        return (dataContext, nodeContext);
    }

    [Fact]
    public async Task ProcessObjectAsync_Simple_OK()
    {
        MapNodeConfiguration mapNodeConfiguration = new()
        {
            Path = "$.data",
            TargetPath = "$.result",
            SelectPaths = new List<string>
            {
                "$.timestamp", "$.batteryPower", "$.productionPower", "$.additionalProductionPower",
                "$.batteryStateOfCharge", "$.consumption", "$.net"
            }
        };


        var (dataContext, nodeContext) = PrepareTest(mapNodeConfiguration);

        var fn = A.Fake<NodeDelegate>();
        var testee = new MapNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.NotNull(dataContext.Current);
        Assert.Equal(6, dataContext.GetSimpleArrayValueByPath<JObject>("$.result")?.Count());
    }
}