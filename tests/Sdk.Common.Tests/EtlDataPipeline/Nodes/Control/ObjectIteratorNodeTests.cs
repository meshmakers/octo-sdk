using FakeItEasy;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Sdk.Common.Tests.Fixtures;

namespace Sdk.Common.Tests.EtlDataPipeline.Nodes.Control;

/// <summary>
/// Test configuration for the concrete ObjectIteratorNode implementation
/// </summary>
[NodeName("TestObjectIterator", 1)]
public record TestObjectIteratorNodeConfiguration
    : ObjectIteratorNodeConfiguration<TokenConfigurationNode>, IChildNodeConfiguration
{
    public ICollection<NodeConfiguration>? Transformations { get; set; }
}

/// <summary>
/// Concrete test implementation of the abstract ObjectIteratorNode
/// </summary>
[NodeConfiguration(typeof(TestObjectIteratorNodeConfiguration))]
public class TestObjectIteratorNode(NodeDelegate next) : ObjectIteratorNode<TokenConfigurationNode>
{
    public override async Task ProcessObjectAsync(IDataContext dataContext, INodeContext nodeContext)
    {
        var c = nodeContext.GetNodeConfiguration<TestObjectIteratorNodeConfiguration>();

        foreach (var selectPathItem in c.SelectPath)
        {
            await ProcessToken(dataContext, nodeContext, next, selectPathItem);
        }
    }
}

public class ObjectIteratorNodeTests : IClassFixture<NodeFixture>
{
    private readonly NodeFixture _fixture;

    public ObjectIteratorNodeTests(NodeFixture fixture)
    {
        _fixture = fixture;
        _fixture.RegisterNode(typeof(TestObjectIteratorNode));
    }

    [Fact]
    public async Task ProcessObjectAsync_WithArray_ProcessesAllElements()
    {
        // Arrange
        var dataContext = new DataContext
        {
            Current = new JArray { 1, 2, 3 }
        };
        var config = new TestObjectIteratorNodeConfiguration
        {
            SelectPath = new List<TokenConfigurationNode>
            {
                new() { Transformations = null }
            }
        };

        var (nodeContext, fn) = PrepareTest(config, dataContext);
        var testee = new TestObjectIteratorNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        Assert.NotNull(dataContext.Current);
        Assert.IsType<JArray>(dataContext.Current);
        var resultArray = (JArray)dataContext.Current;
        Assert.Equal(3, resultArray.Count);
    }

    [Fact]
    public async Task ProcessObjectAsync_WithSingleObject_ProcessesSingleElement()
    {
        // Arrange
        var dataContext = new DataContext
        {
            Current = new JObject { ["key"] = "value" }
        };
        var config = new TestObjectIteratorNodeConfiguration
        {
            SelectPath = new List<TokenConfigurationNode>
            {
                new() { Transformations = null }
            }
        };

        var (nodeContext, fn) = PrepareTest(config, dataContext);
        var testee = new TestObjectIteratorNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        Assert.NotNull(dataContext.Current);
        A.CallTo(() => fn.Invoke(A<IDataContext>._, A<INodeContext>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ProcessObjectAsync_WithEmptyArray_CallsNextWithEmptyArray()
    {
        // Arrange
        var dataContext = new DataContext
        {
            Current = new JArray()
        };
        var config = new TestObjectIteratorNodeConfiguration
        {
            SelectPath = new List<TokenConfigurationNode>
            {
                new() { Transformations = null }
            }
        };

        var (nodeContext, fn) = PrepareTest(config, dataContext);
        var testee = new TestObjectIteratorNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        Assert.NotNull(dataContext.Current);
        Assert.IsType<JArray>(dataContext.Current);
        var resultArray = (JArray)dataContext.Current;
        Assert.Empty(resultArray);
    }

    [Fact]
    public async Task ProcessObjectAsync_WithObjectArray_PreservesObjectData()
    {
        // Arrange
        var dataContext = new DataContext
        {
            Current = new JArray
            {
                new JObject { ["name"] = "Alice", ["age"] = 30 },
                new JObject { ["name"] = "Bob", ["age"] = 25 }
            }
        };
        var config = new TestObjectIteratorNodeConfiguration
        {
            SelectPath = new List<TokenConfigurationNode>
            {
                new() { Transformations = null }
            }
        };

        var (nodeContext, fn) = PrepareTest(config, dataContext);
        var testee = new TestObjectIteratorNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        Assert.NotNull(dataContext.Current);
        Assert.IsType<JArray>(dataContext.Current);
        var resultArray = (JArray)dataContext.Current;
        Assert.Equal(2, resultArray.Count);
    }

    private (INodeContext, NodeDelegate) PrepareTest(TestObjectIteratorNodeConfiguration config,
        DataContext dataContext)
    {
        var logger = A.Fake<IPipelineLogger>();
        var rootNodeContext =
            NodeContext.CreateRootNodeContext(_fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext =
            rootNodeContext.RegisterChildNode("TestObjectIterator", 0, config, dataContext);

        var fn = A.Fake<NodeDelegate>();

        return (nodeContext, fn);
    }
}
