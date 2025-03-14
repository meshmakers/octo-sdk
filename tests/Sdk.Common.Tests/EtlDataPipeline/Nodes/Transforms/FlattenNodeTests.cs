using FakeItEasy;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Sdk.Common.Tests.Fixtures;
using Sdk.Common.Tests.TestData.Dto;

namespace Sdk.Common.Tests.EtlDataPipeline.Nodes.Transforms;

public class FlattenNodeTests(NodeFixture fixture) : IClassFixture<NodeFixture>
{
    private (DataContext, INodeContext) PrepareTest(FlattenNodeConfiguration flattenNodeConfiguration)
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(Generator.GenerateOrder())
        };
        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Flatten", 0, flattenNodeConfiguration, dataContext);
        return (dataContext, nodeContext);
    }
    
    private (DataContext, INodeContext) PrepareTestWithN(FlattenNodeConfiguration flattenNodeConfiguration)
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(Generator.GenerateOrders())
        };
        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Flatten", 0, flattenNodeConfiguration, dataContext);
        return (dataContext, nodeContext);
    }

    [Fact]
    public async Task ProcessObjectAsync_Simple_OK()
    {
        FlattenNodeConfiguration flattenNodeConfiguration = new()
        {
            Path = "$.Items[*].OrderItemId",
            TargetPath = "$.Result",
          
        };


        var (dataContext, nodeContext) = PrepareTest(flattenNodeConfiguration);

        var fn = A.Fake<NodeDelegate>();
        var testee = new FlattenNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
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


        var (dataContext, nodeContext) = PrepareTest(flattenNodeConfiguration);

        var fn = A.Fake<NodeDelegate>();
        var testee = new FlattenNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
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


        var (dataContext, nodeContext) = PrepareTestWithN(flattenNodeConfiguration);

        var fn = A.Fake<NodeDelegate>();
        var testee = new FlattenNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
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


        var (dataContext, nodeContext) = PrepareTestWithN(flattenNodeConfiguration);

        var fn = A.Fake<NodeDelegate>();
        var testee = new FlattenNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.NotNull(dataContext.Current);
        Assert.Equal(10, dataContext.GetSimpleArrayValueByPath<object>("$.Result")?.Count());
    }
}