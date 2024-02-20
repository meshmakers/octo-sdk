using Meshmakers.Octo.Sdk.Common.DataPipeline;
using Meshmakers.Octo.Sdk.Common.DataPipeline.Nodes.Transforms;
using Microsoft.Extensions.DependencyInjection;
using Sdk.Common.Tests.Fixtures;

namespace Sdk.Common.Tests.DataPipeline.Nodes.Signals;

public class LinearScalerNodeTests(ServiceCollectionFixture fixture)
    : IClassFixture<ServiceCollectionFixture>
{
    [Fact]
    public async Task ProcessObjectAsync_100_1000_OK()
    {
        var dataContext = new TransformDataContext(fixture.Services.BuildServiceProvider(), 6);
        dataContext.SetTargetValueByName("$.Test", 6);
        dataContext.SetConfigurationNode(new LinearScalerConfigurationNode
        {
            ScaleInputMin = 0,
            ScaleInputMax = 10,
            ScaleOutputMin = 0,
            ScaleOutputMax = 1000
        });

        var testee = new LinearScalerNode();
        await testee.ProcessObjectAsync(dataContext);

        Assert.Equal(600d, dataContext.GetSourceValueByPath<double>("$.Test"));
    }

    [Fact]
    public async Task ProcessObjectAsync_100_Minus1000_OK()
    {
        var dataContext = new TransformDataContext(fixture.Services.BuildServiceProvider(), 6);
        dataContext.SetTargetValueByName("$.Test", 6);
        dataContext.SetConfigurationNode(new LinearScalerConfigurationNode
        {
            ScaleInputMin = 0,
            ScaleInputMax = 10,
            ScaleOutputMin = 0,
            ScaleOutputMax = -1000
        });

        var testee = new LinearScalerNode();
        await testee.ProcessObjectAsync(dataContext);

        Assert.Equal(-600d, dataContext.GetSourceValueByPath<double>("$.Test"));
    }

    [Fact]
    public async Task ProcessObjectAsync_0_OK()
    {
        var dataContext = new TransformDataContext(fixture.Services.BuildServiceProvider(), 6);
        dataContext.SetTargetValueByName("$.Test", 6);
        dataContext.SetConfigurationNode(new LinearScalerConfigurationNode
        {
            ScaleInputMin = 0,
            ScaleInputMax = 0,
            ScaleOutputMin = 0,
            ScaleOutputMax = 0
        });

        var testee = new LinearScalerNode();
        await testee.ProcessObjectAsync(dataContext);

        Assert.Equal(double.NaN, dataContext.GetSourceValueByPath<double>("$.Test"));
    }
}