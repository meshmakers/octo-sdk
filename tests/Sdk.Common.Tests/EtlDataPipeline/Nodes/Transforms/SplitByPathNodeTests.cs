using FakeItEasy;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Sdk.Common.Tests.Fixtures;
using Sdk.Common.Tests.TestData.Dto;

namespace Sdk.Common.Tests.EtlDataPipeline.Nodes.Transforms;

public class ByPathNodeTests(ServiceCollectionFixture fixture)
    : IClassFixture<ServiceCollectionFixture>
{

    [Fact]
    public async Task ProcessObjectAsync_String_OK()
    {
        var orderDto = Generator.GenerateOrder();
        
        SelectByPathNodeConfiguration selectByPathNodeConfiguration = new()
        {
            Transformations = new List<PathPropertyConfigurationNode>
            {
                new()
                {
                    SourcePath = "$.Customer.Name",
                    TargetPropertyName = "CustomerName"
                }
            }
        };

        var fn = A.Fake<NodeDelegate>();
        var testee = new SelectByPathNode(fn);
        var dataContext = new DataContext(
            fixture.Services.BuildServiceProvider(), fixture.PipelineServices.BuildServiceProvider())
        {
            Current = JObject.FromObject(orderDto)
        };
        dataContext.SetConfigurationNode(selectByPathNodeConfiguration);
        await testee.ProcessObjectAsync(dataContext);

        A.CallTo(() => fn.Invoke(dataContext)).MustHaveHappenedOnceExactly();
        Assert.NotNull(dataContext.Current);
        Assert.Equal(dataContext.Current["CustomerName"], orderDto.Customer.Name);
    }
    
    [Fact]
    public async Task ProcessObjectAsync_Int32_OK()
    {
        var orderDto = Generator.GenerateOrder();
        
        SelectByPathNodeConfiguration selectByPathNodeConfiguration = new()
        {
            Transformations = new List<PathPropertyConfigurationNode>
            {
                new()
                {
                    SourcePath = "$.Customer.Id",
                    TargetPropertyName = "Id"
                }
            }
        };
    
        var fn = A.Fake<NodeDelegate>();
        var testee = new SelectByPathNode(fn);
        var dataContext = new DataContext(
            fixture.Services.BuildServiceProvider(), fixture.PipelineServices.BuildServiceProvider())
        {
            Current = JObject.FromObject(orderDto)
        };
        dataContext.SetConfigurationNode(selectByPathNodeConfiguration);
    
        await testee.ProcessObjectAsync(dataContext);
    
        A.CallTo(() => fn.Invoke(dataContext)).MustHaveHappenedOnceExactly();
        Assert.NotNull(dataContext.Current);
        Assert.Equal(dataContext.Current["Id"], orderDto.Customer.Id);
    }
}