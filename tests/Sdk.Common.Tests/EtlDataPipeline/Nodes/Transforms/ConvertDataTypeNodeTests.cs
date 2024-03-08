using FakeItEasy;
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
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext(
            fixture.Services.BuildServiceProvider(), logger, null)
        {
            Current = new JObject
            {
                ["Value"] = 6
            }
        };

        dataContext.SetNodeConfiguration(new ConvertDataTypeNodeConfiguration
        {
            SourcePath = "$.Value",
            TargetPropertyName = "Demo",
            ValueType = AttributeValueTypesDto.String
        });

        var fn = A.Fake<NodeDelegate>();

        var testee = new ConvertDataTypeNode(fn);
        await testee.ProcessObjectAsync(dataContext);

        A.CallTo(() => fn.Invoke(dataContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("6", dataContext.Current["Demo"]);
    }
}