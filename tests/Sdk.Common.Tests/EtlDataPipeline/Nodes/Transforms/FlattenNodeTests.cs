using FakeItEasy;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Sdk.Common.Tests.Fixtures;
using Sdk.Common.Tests.TestData.Dto;

namespace Sdk.Common.Tests.EtlDataPipeline.Nodes.Transforms;

public class FlattenNodeTests(NodeFixture fixture) : IClassFixture<NodeFixture>
{
    private DataContext PrepareTest(FlattenNodeConfiguration flattenNodeConfiguration)
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext(
            fixture.Services.BuildServiceProvider(), logger)
        {
            Current = JObject.FromObject(Generator.GenerateOrder())
        };
        dataContext.RegisterNode("Flatten", 0, flattenNodeConfiguration);
        return dataContext;
    }
    
    private DataContext PrepareTestWithN(FlattenNodeConfiguration flattenNodeConfiguration)
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext(
            fixture.Services.BuildServiceProvider(), logger)
        {
            Current = JObject.FromObject(Generator.GenerateOrders())
        };
        dataContext.RegisterNode("Flatten", 0, flattenNodeConfiguration);
        return dataContext;
    }

    [Fact]
    public async Task ProcessObjectAsync_Simple_OK()
    {
        FlattenNodeConfiguration flattenNodeConfiguration = new()
        {
            Path = "$.Items[*].OrderItemId",
            TargetPath = "$.Result",
          
        };


        var dataContext = PrepareTest(flattenNodeConfiguration);

        var fn = A.Fake<NodeDelegate>();
        var testee = new FlattenNode(fn);

        await testee.ProcessObjectAsync(dataContext);

        A.CallTo(() => fn.Invoke(dataContext)).MustHaveHappenedOnceExactly();
        Assert.NotNull(dataContext.Current);
        Assert.Equal(3, dataContext.GetSimpleArrayValueByPath<int>("$.Result")?.Count());
    }
    
    [Fact]
    public async Task ProcessObjectAsync_Complex_OK()
    {
        FlattenNodeConfiguration flattenNodeConfiguration = new()
        {
            Path = "$.Items[*].Product",
            TargetPath = "$.Result",
          
        };


        var dataContext = PrepareTest(flattenNodeConfiguration);

        var fn = A.Fake<NodeDelegate>();
        var testee = new FlattenNode(fn);

        await testee.ProcessObjectAsync(dataContext);

        A.CallTo(() => fn.Invoke(dataContext)).MustHaveHappenedOnceExactly();
        Assert.NotNull(dataContext.Current);
        Assert.Equal(3, dataContext.GetSimpleArrayValueByPath<object>("$.Result")?.Count());
    }
    
    [Fact]
    public async Task ProcessObjectAsync_Array_ByJsonPath_OK()
    {
        FlattenNodeConfiguration flattenNodeConfiguration = new()
        {
            Path = "$.Orders[*].Items[*]",
            TargetPath = "$.Result",
          
        };


        var dataContext = PrepareTestWithN(flattenNodeConfiguration);

        var fn = A.Fake<NodeDelegate>();
        var testee = new FlattenNode(fn);

        await testee.ProcessObjectAsync(dataContext);

        A.CallTo(() => fn.Invoke(dataContext)).MustHaveHappenedOnceExactly();
        Assert.NotNull(dataContext.Current);
        Assert.Equal(30, dataContext.GetSimpleArrayValueByPath<object>("$.Result")?.Count());
    }
    
    [Fact]
    public async Task ProcessObjectAsync_WithoutJsonPath_OK()
    {
        FlattenNodeConfiguration flattenNodeConfiguration = new()
        {
            Path = "$.Orders[*].Items",
            TargetPath = "$.Result",
        };


        var dataContext = PrepareTestWithN(flattenNodeConfiguration);

        var fn = A.Fake<NodeDelegate>();
        var testee = new FlattenNode(fn);

        await testee.ProcessObjectAsync(dataContext);

        A.CallTo(() => fn.Invoke(dataContext)).MustHaveHappenedOnceExactly();
        Assert.NotNull(dataContext.Current);
        Assert.Equal(10, dataContext.GetSimpleArrayValueByPath<object>("$.Result")?.Count());
    }
}