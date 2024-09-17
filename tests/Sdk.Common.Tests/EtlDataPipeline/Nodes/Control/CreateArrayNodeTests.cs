using FakeItEasy;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Sdk.Common.Tests.Fixtures;
using Sdk.Common.Tests.TestData;
using Sdk.Common.Tests.TestData.Dto;

namespace Sdk.Common.Tests.EtlDataPipeline.Nodes.Control;

public class CreateArrayNodeTests(NodeFixture fixture) : IClassFixture<NodeFixture>
{
    private DataContext PrepareTest(CreateArrayNodeConfiguration createArrayNodeConfiguration)
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext(
            fixture.Services.BuildServiceProvider(), logger)
        {
            Current = JObject.FromObject(fixture.OrderDto)
        };
        dataContext.RegisterNode("CreateArray", 0, createArrayNodeConfiguration);
        return dataContext;
    }

    [Fact]
    public async Task ProcessObjectAsync_Simple_OK()
    {
        CreateArrayNodeConfiguration createArrayNodeConfiguration = new()
        {
            Path = "$.Items[*].OrderItemId",
            TargetPath = "$.Result",
          
        };


        var dataContext = PrepareTest(createArrayNodeConfiguration);

        var fn = A.Fake<NodeDelegate>();
        var testee = new CreateArrayNode(fn);

        await testee.ProcessObjectAsync(dataContext);

        A.CallTo(() => fn.Invoke(dataContext)).MustHaveHappenedOnceExactly();
        Assert.NotNull(dataContext.Current);
        Assert.Equal(3, dataContext.GetSimpleArrayValueByPath<int>("$.Result")?.Count());
    }
    
    [Fact]
    public async Task ProcessObjectAsync_Complex_OK()
    {
        CreateArrayNodeConfiguration createArrayNodeConfiguration = new()
        {
            Path = "$.Items[*].Product",
            TargetPath = "$.Result",
          
        };


        var dataContext = PrepareTest(createArrayNodeConfiguration);

        var fn = A.Fake<NodeDelegate>();
        var testee = new CreateArrayNode(fn);

        await testee.ProcessObjectAsync(dataContext);

        A.CallTo(() => fn.Invoke(dataContext)).MustHaveHappenedOnceExactly();
        Assert.NotNull(dataContext.Current);
        Assert.Equal(3, dataContext.GetSimpleArrayValueByPath<object>("$.Result")?.Count());
    }
}