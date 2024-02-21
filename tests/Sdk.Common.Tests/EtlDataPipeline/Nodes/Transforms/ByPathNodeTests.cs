using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;
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
        
        ByPathNodeConfiguration byPathNodeConfiguration = new()
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

        var testee = new ByPathNode();
        var dataContext = new TransformDataContext(fixture.Services.BuildServiceProvider(),
            fixture.PipelineServices.BuildServiceProvider(), JObject.FromObject(orderDto));
        dataContext.SetConfigurationNode(byPathNodeConfiguration);
        await testee.ProcessObjectAsync(dataContext);

        Assert.NotNull(dataContext.Target);
        Assert.Equal(dataContext.Target["CustomerName"], orderDto.Customer.Name);
    }
    
    [Fact]
    public async Task ProcessObjectAsync_Int32_OK()
    {
        var orderDto = Generator.GenerateOrder();
        
        ByPathNodeConfiguration byPathNodeConfiguration = new()
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
    
        var testee = new ByPathNode();
        var dataContext = new TransformDataContext(
            fixture.Services.BuildServiceProvider(), 
            fixture.PipelineServices.BuildServiceProvider(),JObject.FromObject(orderDto));
        dataContext.SetConfigurationNode(byPathNodeConfiguration);
    
        await testee.ProcessObjectAsync(dataContext);
    
        Assert.NotNull(dataContext.Target);
        Assert.Equal(dataContext.Target["Id"], orderDto.Customer.Id);
    }
}