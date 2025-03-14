using FakeItEasy;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Sdk.Common.Tests.Fixtures;

namespace Sdk.Common.Tests.EtlDataPipeline.Nodes.Transforms;

public class LinearScalerNodeTests(ServiceCollectionFixture fixture)
    : IClassFixture<ServiceCollectionFixture>
{
    [Fact]
    public async Task ProcessObjectAsync_WithPath_OK()
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = new JObject
            {
                ["Value"] = 6
            }
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("LinearScaler", 0, new LinearScalerNodeConfiguration
        {
            Path = "$.Value",
            TargetPath = "Demo",
            ScaleInputMin = 0,
            ScaleInputMax = 10,
            ScaleOutputMin = 0,
            ScaleOutputMax = 1000
        }, dataContext);

        var fn = A.Fake<NodeDelegate>();
        var testee = new LinearScalerNode(fn);
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(600d, dataContext.Current["Demo"]);
    }

    [Fact]
    public async Task ProcessObjectAsync_100_1000_OK()
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = 6
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("LinearScaler", 0, new LinearScalerNodeConfiguration
        {
            ScaleInputMin = 0,
            ScaleInputMax = 10,
            ScaleOutputMin = 0,
            ScaleOutputMax = 1000
        }, dataContext);

        var fn = A.Fake<NodeDelegate>();
        var testee = new LinearScalerNode(fn);
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(600d, dataContext.Current);
    }

    [Fact]
    public async Task ProcessObjectAsync_100_Minus1000_OK()
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = 6
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("LinearScaler", 0, new LinearScalerNodeConfiguration
        {
            ScaleInputMin = 0,
            ScaleInputMax = 10,
            ScaleOutputMin = 0,
            ScaleOutputMax = -1000
        }, dataContext);

        var fn = A.Fake<NodeDelegate>();
        var testee = new LinearScalerNode(fn);
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(-600d, dataContext.Current);
    }

    [Fact]
    public async Task ProcessObjectAsync_0_OK()
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = 6
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("LinearScaler", 0, new LinearScalerNodeConfiguration
        {
            ScaleInputMin = 0,
            ScaleInputMax = 0,
            ScaleOutputMin = 0,
            ScaleOutputMax = 0
        }, dataContext);

        var fn = A.Fake<NodeDelegate>();
        var testee = new LinearScalerNode(fn);
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(double.NaN, dataContext.Current);
    }
}