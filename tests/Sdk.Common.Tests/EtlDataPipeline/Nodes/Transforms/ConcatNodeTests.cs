using FakeItEasy;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Sdk.Common.Tests.Fixtures;

namespace Sdk.Common.Tests.EtlDataPipeline.Nodes.Transforms;

public class ConcatNodeTests(ServiceCollectionFixture fixture)
    : IClassFixture<ServiceCollectionFixture>
{
    [Fact]
    public async Task ProcessObjectAsync_WithStaticValues_ConcatenatesStrings()
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = new JObject
            {
                ["name"] = "test"
            }
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Concat", 0, new ConcatNodeConfiguration
        {
            Path = "$",
            ConcatSubPath = "result",
            Parts = new List<ConcatItem>
            {
                new() { Value = "Hello" },
                new() { Value = " " },
                new() { Value = "World" }
            }
        }, dataContext);

        var fn = A.Fake<NodeDelegate>();
        var testee = new ConcatNode(fn);
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("Hello World", dataContext.Current["result"]?.Value<string>());
    }

    [Fact]
    public async Task ProcessObjectAsync_WithValuePaths_ConcatenatesFromPaths()
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = new JObject
            {
                ["firstName"] = "John",
                ["lastName"] = "Doe"
            }
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Concat", 0, new ConcatNodeConfiguration
        {
            Path = "$",
            ConcatSubPath = "fullName",
            Parts = new List<ConcatItem>
            {
                new() { ValuePath = "$.firstName" },
                new() { Value = " " },
                new() { ValuePath = "$.lastName" }
            }
        }, dataContext);

        var fn = A.Fake<NodeDelegate>();
        var testee = new ConcatNode(fn);
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("John Doe", dataContext.Current["fullName"]?.Value<string>());
    }

    [Fact]
    public async Task ProcessObjectAsync_WithMixedValuesAndPaths_ConcatenatesCorrectly()
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = new JObject
            {
                ["name"] = "Alice"
            }
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Concat", 0, new ConcatNodeConfiguration
        {
            Path = "$",
            ConcatSubPath = "greeting",
            Parts = new List<ConcatItem>
            {
                new() { Value = "Hello, " },
                new() { ValuePath = "$.name" },
                new() { Value = "!" }
            }
        }, dataContext);

        var fn = A.Fake<NodeDelegate>();
        var testee = new ConcatNode(fn);
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("Hello, Alice!", dataContext.Current["greeting"]?.Value<string>());
    }

    [Fact]
    public async Task ProcessObjectAsync_WithEmptyParts_ReturnsEmptyString()
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = new JObject
            {
                ["name"] = "test"
            }
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Concat", 0, new ConcatNodeConfiguration
        {
            Path = "$",
            ConcatSubPath = "result",
            Parts = new List<ConcatItem>()
        }, dataContext);

        var fn = A.Fake<NodeDelegate>();
        var testee = new ConcatNode(fn);
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("", dataContext.Current["result"]?.Value<string>());
    }

    [Fact]
    public async Task ProcessObjectAsync_WithArrayPath_ConcatenatesForEachElement()
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = new JObject
            {
                ["items"] = new JArray
                {
                    new JObject { ["prefix"] = "A", ["suffix"] = "1" },
                    new JObject { ["prefix"] = "B", ["suffix"] = "2" }
                }
            }
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Concat", 0, new ConcatNodeConfiguration
        {
            Path = "$.items[*]",
            ConcatSubPath = "combined",
            Parts = new List<ConcatItem>
            {
                new() { ValuePath = "$.prefix" },
                new() { Value = "-" },
                new() { ValuePath = "$.suffix" }
            }
        }, dataContext);

        var fn = A.Fake<NodeDelegate>();
        var testee = new ConcatNode(fn);
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        var items = dataContext.Current["items"] as JArray;
        Assert.NotNull(items);
        Assert.Equal("A-1", items[0]?["combined"]?.Value<string>());
        Assert.Equal("B-2", items[1]?["combined"]?.Value<string>());
    }
}
