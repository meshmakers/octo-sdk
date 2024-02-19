using Meshmakers.Octo.Sdk.Common.DataPipeline;
using Meshmakers.Octo.Sdk.Common.DataPipeline.Nodes.Signals;
using Microsoft.Extensions.DependencyInjection;
using Sdk.Common.Tests.Fixtures;

namespace Sdk.Common.Tests.DataPipeline.Nodes.Signals;

public class LinearScalerNodeTests(ServiceCollectionFixture fixture)
    : IClassFixture<ServiceCollectionFixture>
{
    
    [Fact]
    public void ProcessSignalAsync_100_1000_OK()
    {
        var dataContext = new SignalDataContext(fixture.Services.BuildServiceProvider(), 6);
        dataContext.SetConfigurationNode(new LinearScalerConfigurationNode
        {
            ScaleInputMin = 0,
            ScaleInputMax = 10,
            ScaleOutputMin = 0,
            ScaleOutputMax = 1000
        });
        
        var testee = new LinearScalerNode();
        var result = testee.ProcessSignalAsync(dataContext).Result;
        
        Assert.Equal(600d, result);
    }
    
    [Fact]
    public void ProcessSignalAsync_100_Minus1000_OK()
    {
        var dataContext = new SignalDataContext(fixture.Services.BuildServiceProvider(), 6);
        dataContext.SetConfigurationNode(new LinearScalerConfigurationNode
        {
            ScaleInputMin = 0,
            ScaleInputMax = 10,
            ScaleOutputMin = 0,
            ScaleOutputMax = -1000
        });
        
        var testee = new LinearScalerNode();
        var result = testee.ProcessSignalAsync(dataContext).Result;
        
        Assert.Equal(-600d, result);
    }
    
    [Fact]
    public void ProcessSignalAsync_0_OK()
    {
        var dataContext = new SignalDataContext(fixture.Services.BuildServiceProvider(), 6);
        dataContext.SetConfigurationNode(new LinearScalerConfigurationNode
        {
            ScaleInputMin = 0,
            ScaleInputMax = 0,
            ScaleOutputMin = 0,
            ScaleOutputMax = 0
        });
        
        var testee = new LinearScalerNode();
        var result = testee.ProcessSignalAsync(dataContext).Result;
        
        Assert.Equal(double.NaN, result);
    }
}