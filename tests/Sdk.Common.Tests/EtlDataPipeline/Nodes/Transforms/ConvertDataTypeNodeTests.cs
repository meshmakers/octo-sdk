using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Sdk.Common.Tests.Fixtures;

namespace Sdk.Common.Tests.EtlDataPipeline.Nodes.Transforms;

public class ConvertDataTypeNodeTests(ServiceCollectionFixture fixture)
    : IClassFixture<ServiceCollectionFixture>
{
    [Fact]
    public async Task ProcessObjectAsync_WithPath_OK()
    {
        var dataContext = new TransformDataContext(
            fixture.Services.BuildServiceProvider(), fixture.PipelineServices.BuildServiceProvider(), new JObject
            {
                ["Value"] = 6
            });

        dataContext.SetConfigurationNode(new ConvertDataTypeNodeConfiguration
        {
            SourcePath = "$.Value",
            TargetPropertyName = "Demo",
            ValueType = AttributeValueTypesDto.String
        });

        var testee = new ConvertDataTypeNode();
        await testee.ProcessObjectAsync(dataContext);

        Assert.Equal("6", dataContext.Target["Demo"]);
    }
}