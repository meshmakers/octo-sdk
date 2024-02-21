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
        var dataContext = new TransformDataContext(
            fixture.Services.BuildServiceProvider(),
            fixture.PipelineServices.BuildServiceProvider(),
            new JObject
            {
                ["Value"] = 6
            });

        dataContext.SetConfigurationNode(new LinearScalerNodeConfiguration
        {
            SourcePath = "$.Value",
            TargetPropertyName = "Demo",
            ScaleInputMin = 0,
            ScaleInputMax = 10,
            ScaleOutputMin = 0,
            ScaleOutputMax = 1000
        });

        var testee = new LinearScalerNode();
        await testee.ProcessObjectAsync(dataContext);

        Assert.Equal(600d, dataContext.Target["Demo"]);
    }

    [Fact]
    public async Task ProcessObjectAsync_100_1000_OK()
    {
        var dataContext = new TransformDataContext(
            fixture.Services.BuildServiceProvider(), 
            fixture.PipelineServices.BuildServiceProvider(), 6);
        dataContext.SetConfigurationNode(new LinearScalerNodeConfiguration
        {
            ScaleInputMin = 0,
            ScaleInputMax = 10,
            ScaleOutputMin = 0,
            ScaleOutputMax = 1000
        });

        var testee = new LinearScalerNode();
        await testee.ProcessObjectAsync(dataContext);

        Assert.Equal(600d, dataContext.Target);
    }

    [Fact]
    public async Task ProcessObjectAsync_100_Minus1000_OK()
    {
        var dataContext = new TransformDataContext(
            fixture.Services.BuildServiceProvider(),
            fixture.PipelineServices.BuildServiceProvider(),6);
        dataContext.SetConfigurationNode(new LinearScalerNodeConfiguration
        {
            ScaleInputMin = 0,
            ScaleInputMax = 10,
            ScaleOutputMin = 0,
            ScaleOutputMax = -1000
        });

        var testee = new LinearScalerNode();
        await testee.ProcessObjectAsync(dataContext);

        Assert.Equal(-600d, dataContext.Target);
    }

    [Fact]
    public async Task ProcessObjectAsync_0_OK()
    {
        var dataContext = new TransformDataContext(
            fixture.Services.BuildServiceProvider(),
            fixture.PipelineServices.BuildServiceProvider(), 6);
        dataContext.SetConfigurationNode(new LinearScalerNodeConfiguration
        {
            ScaleInputMin = 0,
            ScaleInputMax = 0,
            ScaleOutputMin = 0,
            ScaleOutputMax = 0
        });

        var testee = new LinearScalerNode();
        await testee.ProcessObjectAsync(dataContext);

        Assert.Equal(double.NaN, dataContext.Target);
    }
}