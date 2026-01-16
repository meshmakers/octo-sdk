using FakeItEasy;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Extracts;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Sdk.Common.Tests.Fixtures;

namespace Sdk.Common.Tests.EtlDataPipeline.Nodes.Extracts;

public class SetJsonNodeTests(ServiceCollectionFixture fixture)
    : IClassFixture<ServiceCollectionFixture>
{
    [Fact]
    public async Task ProcessObjectAsync_WithSimpleJsonObject_SetsValue()
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = new JObject()
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("SetJson", 0, new SetJsonNodeConfiguration
        {
            TargetPath = "$.data",
            JsonString = "{\"name\":\"test\",\"value\":42}"
        }, dataContext);

        var fn = A.Fake<NodeDelegate>();
        var testee = new SetJsonNode(fn);
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        var data = dataContext.Current["data"] as JObject;
        Assert.NotNull(data);
        Assert.Equal("test", data["name"]?.Value<string>());
        Assert.Equal(42, data["value"]?.Value<int>());
    }

    [Fact]
    public async Task ProcessObjectAsync_WithNestedJsonObject_SetsValue()
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = new JObject()
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("SetJson", 0, new SetJsonNodeConfiguration
        {
            TargetPath = "$.config",
            JsonString = "{\"settings\":{\"enabled\":true,\"timeout\":30}}"
        }, dataContext);

        var fn = A.Fake<NodeDelegate>();
        var testee = new SetJsonNode(fn);
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        var config = dataContext.Current["config"] as JObject;
        Assert.NotNull(config);
        var settings = config["settings"] as JObject;
        Assert.NotNull(settings);
        Assert.True(settings["enabled"]?.Value<bool>());
        Assert.Equal(30, settings["timeout"]?.Value<int>());
    }

    [Fact]
    public async Task ProcessObjectAsync_WithEmptyJsonObject_SetsEmptyObject()
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = new JObject()
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("SetJson", 0, new SetJsonNodeConfiguration
        {
            TargetPath = "$.empty",
            JsonString = "{}"
        }, dataContext);

        var fn = A.Fake<NodeDelegate>();
        var testee = new SetJsonNode(fn);
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        var empty = dataContext.Current["empty"] as JObject;
        Assert.NotNull(empty);
        Assert.Empty(empty);
    }

    [Fact]
    public async Task ProcessObjectAsync_WithJsonArray_ThrowsException()
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = new JObject()
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("SetJson", 0, new SetJsonNodeConfiguration
        {
            TargetPath = "$.data",
            JsonString = "[1,2,3]"
        }, dataContext);

        var fn = A.Fake<NodeDelegate>();
        var testee = new SetJsonNode(fn);

        // JObject.Parse will throw for arrays
        await Assert.ThrowsAsync<Newtonsoft.Json.JsonReaderException>(
            () => testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_WithInvalidJson_ThrowsException()
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = new JObject()
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("SetJson", 0, new SetJsonNodeConfiguration
        {
            TargetPath = "$.data",
            JsonString = "invalid json"
        }, dataContext);

        var fn = A.Fake<NodeDelegate>();
        var testee = new SetJsonNode(fn);

        await Assert.ThrowsAsync<Newtonsoft.Json.JsonReaderException>(
            () => testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_OverwritesExistingValue()
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = new JObject
            {
                ["data"] = new JObject { ["old"] = "value" }
            }
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("SetJson", 0, new SetJsonNodeConfiguration
        {
            TargetPath = "$.data",
            JsonString = "{\"new\":\"value\"}"
        }, dataContext);

        var fn = A.Fake<NodeDelegate>();
        var testee = new SetJsonNode(fn);
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        var data = dataContext.Current["data"] as JObject;
        Assert.NotNull(data);
        Assert.Equal("value", data["new"]?.Value<string>());
        Assert.Null(data["old"]);
    }
}
