using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Sdk.Common.Tests.Dto;
using Sdk.Common.Tests.Fixtures;

namespace Sdk.Common.Tests.EtlDataPipeline.Nodes.Transforms;

public class ByPathTransformNodeTests(ServiceCollectionFixture fixture)
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
                    TargetPropertyName = "CustomerName",
                    ValueType = AttributeValueTypesDto.String
                }
            }
        };

        var testee = new ByPathTransformNode();
        var dataContext = new TransformDataContext(fixture.Services.BuildServiceProvider(), JObject.FromObject(orderDto));
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
                    TargetPropertyName = "Id",
                    ValueType = AttributeValueTypesDto.Int
                }
            }
        };
    
        var testee = new ByPathTransformNode();
        var dataContext = new TransformDataContext(fixture.Services.BuildServiceProvider(), JObject.FromObject(orderDto));
        dataContext.SetConfigurationNode(byPathNodeConfiguration);
    
        await testee.ProcessObjectAsync(dataContext);
    
        Assert.NotNull(dataContext.Target);
        Assert.Equal(dataContext.Target["Id"], orderDto.Customer.Id);
    }
}