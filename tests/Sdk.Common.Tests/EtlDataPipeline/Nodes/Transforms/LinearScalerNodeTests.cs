using FakeItEasy;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
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
        var dataContext = new DataContext(
            fixture.Services.BuildServiceProvider(), fixture.PipelineServices.BuildServiceProvider())
        {
            Current = new JObject
            {
                ["Value"] = 6
            }
        };

        dataContext.SetNodeConfiguration(new LinearScalerNodeConfiguration
        {
            SourcePath = "$.Value",
            TargetPropertyName = "Demo",
            ScaleInputMin = 0,
            ScaleInputMax = 10,
            ScaleOutputMin = 0,
            ScaleOutputMax = 1000
        });

        var fn = A.Fake<NodeDelegate>();
        var testee = new LinearScalerNode(fn);
        await testee.ProcessObjectAsync(dataContext);

        A.CallTo(() => fn.Invoke(dataContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(600d, dataContext.Current["Demo"]);
    }

    [Fact]
    public async Task ProcessObjectAsync_100_1000_OK()
    {
        var dataContext = new DataContext(
            fixture.Services.BuildServiceProvider(), fixture.PipelineServices.BuildServiceProvider())
        {
            Current = 6
        };
        dataContext.SetNodeConfiguration(new LinearScalerNodeConfiguration
        {
            ScaleInputMin = 0,
            ScaleInputMax = 10,
            ScaleOutputMin = 0,
            ScaleOutputMax = 1000
        });

        var fn = A.Fake<NodeDelegate>();
        var testee = new LinearScalerNode(fn);
        await testee.ProcessObjectAsync(dataContext);

        A.CallTo(() => fn.Invoke(dataContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(600d, dataContext.Current);
    }

    [Fact]
    public async Task ProcessObjectAsync_100_Minus1000_OK()
    {
        var dataContext = new DataContext(
            fixture.Services.BuildServiceProvider(), fixture.PipelineServices.BuildServiceProvider())
        {
            Current = 6
        };
        dataContext.SetNodeConfiguration(new LinearScalerNodeConfiguration
        {
            ScaleInputMin = 0,
            ScaleInputMax = 10,
            ScaleOutputMin = 0,
            ScaleOutputMax = -1000
        });

        var fn = A.Fake<NodeDelegate>();
        var testee = new LinearScalerNode(fn);
        await testee.ProcessObjectAsync(dataContext);

        A.CallTo(() => fn.Invoke(dataContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(-600d, dataContext.Current);
    }

    [Fact]
    public async Task ProcessObjectAsync_0_OK()
    {
        var dataContext = new DataContext(
            fixture.Services.BuildServiceProvider(), fixture.PipelineServices.BuildServiceProvider())
        {
            Current = 6
        };
        dataContext.SetNodeConfiguration(new LinearScalerNodeConfiguration
        {
            ScaleInputMin = 0,
            ScaleInputMax = 0,
            ScaleOutputMin = 0,
            ScaleOutputMax = 0
        });

        var fn = A.Fake<NodeDelegate>();
        var testee = new LinearScalerNode(fn);
        await testee.ProcessObjectAsync(dataContext);

        A.CallTo(() => fn.Invoke(dataContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(double.NaN, dataContext.Current);
    }
}