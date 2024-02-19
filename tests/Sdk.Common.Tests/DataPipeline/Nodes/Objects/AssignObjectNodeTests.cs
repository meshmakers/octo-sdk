using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.Common.DataPipeline;
using Meshmakers.Octo.Sdk.Common.DataPipeline.Nodes.Objects;
using Microsoft.Extensions.DependencyInjection;
using Sdk.Common.Tests.Dto;
using Sdk.Common.Tests.Fixtures;

namespace Sdk.Common.Tests.DataPipeline.Nodes.Objects;

public class AssignObjectNodeTests(ServiceCollectionFixture fixture)
    : IClassFixture<ServiceCollectionFixture>
{

    [Fact]
    public async Task ProcessObjectAsync_String_OK()
    {
        var orderDto = Generator.GenerateOrder();
        
        AssignObjectConfigurationNode assignObjectConfigurationNode = new()
        {
            TransformList = new List<AssignObjectTransformationNode>
            {
                new()
                {
                    Path = "$.Customer.Name",
                    Name = "CustomerName",
                    ValueType = AttributeValueTypesDto.String
                }
            }
        };

        var testee = new AssignObjectNode();
        var dataContext = new ObjectDataContext(fixture.Services.BuildServiceProvider(), orderDto);
        dataContext.SetConfigurationNode(assignObjectConfigurationNode);

        var r = await testee.ProcessObjectAsync(dataContext);

        Assert.NotNull(r);
        var rtEntity = (RtEntityDto)r;
        Assert.Equal(rtEntity.Properties?["CustomerName"], orderDto.Customer.Name);
    }
    
    [Fact]
    public async Task ProcessObjectAsync_Int32_OK()
    {
        var orderDto = Generator.GenerateOrder();
        
        AssignObjectConfigurationNode assignObjectConfigurationNode = new()
        {
            TransformList = new List<AssignObjectTransformationNode>
            {
                new()
                {
                    Path = "$.Customer.Id",
                    Name = "Id",
                    ValueType = AttributeValueTypesDto.Int
                }
            }
        };

        var testee = new AssignObjectNode();
        var dataContext = new ObjectDataContext(fixture.Services.BuildServiceProvider(), orderDto);
        dataContext.SetConfigurationNode(assignObjectConfigurationNode);

        var r = await testee.ProcessObjectAsync(dataContext);

        Assert.NotNull(r);
        var rtEntity = (RtEntityDto)r;
        Assert.Equal(rtEntity.Properties?["Id"], orderDto.Customer.Id);
    }
}