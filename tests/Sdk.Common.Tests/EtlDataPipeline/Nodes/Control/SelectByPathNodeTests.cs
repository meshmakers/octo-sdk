using FakeItEasy;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Sdk.Common.Tests.Fixtures;
using Sdk.Common.Tests.TestData;

namespace Sdk.Common.Tests.EtlDataPipeline.Nodes.Control;

public class SelectByPathNodeTests(NodeFixture fixture)
    : IClassFixture<NodeFixture>
{
    private DataContext PrepareTest(SelectByPathNodeConfiguration selectByPathNodeConfiguration)
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext(
            fixture.Services.BuildServiceProvider(), logger)
        {
            Current = JObject.FromObject(fixture.OrderDto)
        };
        dataContext.RegisterNode("SelectByPath", 0, selectByPathNodeConfiguration);
        return dataContext;
    }
    
    [Fact]
    public async Task ProcessObjectAsync_Object_String_NoTransforms_OK()
    {
        SelectByPathNodeConfiguration selectByPathNodeConfiguration = new()
        {
            SelectPath = new List<PathPropertyConfigurationNode>
            {
                new()
                {
                    Path = "$.Customer.Name",
                    TargetPath = "CustomerName"
                   
                }
            }
        };

        var dataContext = PrepareTest(selectByPathNodeConfiguration);

        var fn = A.Fake<NodeDelegate>();
        var testee = new SelectByPathNode(fn);

        await testee.ProcessObjectAsync(dataContext);

        A.CallTo(() => fn.Invoke(dataContext)).MustHaveHappenedOnceExactly();
        Assert.NotNull(dataContext.Current);
        Assert.Equal(dataContext.Current["CustomerName"], fixture.OrderDto.Customer.Name);
    }
    
    [Fact]
    public async Task ProcessObjectAsync_Object_String_WithTransforms_OK()
    {
        SelectByPathNodeConfiguration selectByPathNodeConfiguration = new()
        {
            SelectPath = new List<PathPropertyConfigurationNode>
            {
                new()
                {
                    Path = "$.Customer.Name",
                    TargetPath = "TestProperty",
                    Transformations = new List<NodeConfiguration>
                    {
                        new TestNodeConfiguration()
                    }
                }
            }
        };
        
        var testCounter = A.Fake<ITestCounter>();
        fixture.Services.AddSingleton(testCounter);
        A.CallTo(() => testCounter.GetNext()).Returns(567);

        var dataContext = PrepareTest(selectByPathNodeConfiguration);

        var fn = A.Fake<NodeDelegate>();
        var testee = new SelectByPathNode(fn);

        await testee.ProcessObjectAsync(dataContext);

        A.CallTo(() => testCounter.GetNext()).MustHaveHappened(1, Times.Exactly);
        A.CallTo(() => fn.Invoke(dataContext)).MustHaveHappenedOnceExactly();
        Assert.NotNull(dataContext.Current);
        Assert.Equal(dataContext.Current["TestProperty"], 567);
    }
    
    [Fact]
    public async Task ProcessObjectAsync_Object_Int32_NoTransforms_OK()
    {
        SelectByPathNodeConfiguration selectByPathNodeConfiguration = new()
        {
            SelectPath = new List<PathPropertyConfigurationNode>
            {
                new()
                {
                    Path = "$.Customer.Id",
                    TargetPath = "CustomerId"
                }
            }
        };
        
        var dataContext = PrepareTest(selectByPathNodeConfiguration);

        var fn = A.Fake<NodeDelegate>();
        var testee = new SelectByPathNode(fn);

        await testee.ProcessObjectAsync(dataContext);

        A.CallTo(() => fn.Invoke(dataContext)).MustHaveHappenedOnceExactly();
        Assert.NotNull(dataContext.Current);
        Assert.Equal(dataContext.Current["CustomerId"], fixture.OrderDto.Customer.Id);
    }

    [Fact]
    public async Task ProcessObjectAsync_Object_Int32_WithTransforms_OK()
    {
        SelectByPathNodeConfiguration selectByPathNodeConfiguration = new()
        {
            SelectPath = new List<PathPropertyConfigurationNode>
            {
                new()
                {
                    Path = "$.Customer.Id",
                    TargetPath = "TestProperty",
                    Transformations = new List<NodeConfiguration>
                    {
                        new TestNodeConfiguration()
                    }
                }
            }
        };
        
        var testCounter = A.Fake<ITestCounter>();
        fixture.Services.AddSingleton(testCounter);
        A.CallTo(() => testCounter.GetNext()).Returns(987);

        var dataContext = PrepareTest(selectByPathNodeConfiguration);

        var fn = A.Fake<NodeDelegate>();
        var testee = new SelectByPathNode(fn);

        await testee.ProcessObjectAsync(dataContext);

        A.CallTo(() => testCounter.GetNext()).MustHaveHappened(1, Times.Exactly);
        A.CallTo(() => fn.Invoke(dataContext)).MustHaveHappenedOnceExactly();
        Assert.NotNull(dataContext.Current);
        Assert.Equal(dataContext.Current["TestProperty"], "987");
    }
}