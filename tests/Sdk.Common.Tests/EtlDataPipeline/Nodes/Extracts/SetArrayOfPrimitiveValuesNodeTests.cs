using FakeItEasy;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Extracts;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Sdk.Common.Tests.Fixtures;

namespace Sdk.Common.Tests.EtlDataPipeline.Nodes.Extracts;

public class SetArrayOfPrimitiveValuesNodeTests(ServiceCollectionFixture fixture)
    : IClassFixture<ServiceCollectionFixture>
{
    [Fact]
    public async Task ProcessObjectAsync_WithIntegerArray_SetsValues()
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = new JObject()
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("SetArrayOfPrimitiveValues", 0,
            new SetArrayOfPrimitiveValuesNodeConfiguration
            {
                TargetPath = "$.numbers",
                Values = new object[] { 1, 2, 3, 4, 5 }
            }, dataContext);

        var fn = A.Fake<NodeDelegate>();
        var testee = new SetArrayOfPrimitiveValuesNode(fn);
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        var numbers = dataContext.Current["numbers"] as JArray;
        Assert.NotNull(numbers);
        Assert.Equal(5, numbers.Count);
        Assert.Equal(1, numbers[0]?.Value<int>());
        Assert.Equal(5, numbers[4]?.Value<int>());
    }

    [Fact]
    public async Task ProcessObjectAsync_WithStringArray_SetsValues()
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = new JObject()
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("SetArrayOfPrimitiveValues", 0,
            new SetArrayOfPrimitiveValuesNodeConfiguration
            {
                TargetPath = "$.names",
                Values = new object[] { "Alice", "Bob", "Charlie" }
            }, dataContext);

        var fn = A.Fake<NodeDelegate>();
        var testee = new SetArrayOfPrimitiveValuesNode(fn);
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        var names = dataContext.Current["names"] as JArray;
        Assert.NotNull(names);
        Assert.Equal(3, names.Count);
        Assert.Equal("Alice", names[0]?.Value<string>());
        Assert.Equal("Charlie", names[2]?.Value<string>());
    }

    [Fact]
    public async Task ProcessObjectAsync_WithMixedArray_SetsValues()
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = new JObject()
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("SetArrayOfPrimitiveValues", 0,
            new SetArrayOfPrimitiveValuesNodeConfiguration
            {
                TargetPath = "$.mixed",
                Values = new object[] { 1, "two", 3.0, true }
            }, dataContext);

        var fn = A.Fake<NodeDelegate>();
        var testee = new SetArrayOfPrimitiveValuesNode(fn);
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        var mixed = dataContext.Current["mixed"] as JArray;
        Assert.NotNull(mixed);
        Assert.Equal(4, mixed.Count);
        Assert.Equal(1, mixed[0]?.Value<int>());
        Assert.Equal("two", mixed[1]?.Value<string>());
        Assert.Equal(3.0, mixed[2]?.Value<double>());
        Assert.True(mixed[3]?.Value<bool>());
    }

    [Fact]
    public async Task ProcessObjectAsync_WithEmptyArray_SetsEmptyArray()
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = new JObject()
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("SetArrayOfPrimitiveValues", 0,
            new SetArrayOfPrimitiveValuesNodeConfiguration
            {
                TargetPath = "$.empty",
                Values = Array.Empty<object>()
            }, dataContext);

        var fn = A.Fake<NodeDelegate>();
        var testee = new SetArrayOfPrimitiveValuesNode(fn);
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        var empty = dataContext.Current["empty"] as JArray;
        Assert.NotNull(empty);
        Assert.Empty(empty);
    }

    [Fact]
    public async Task ProcessObjectAsync_WithDoubleArray_SetsValues()
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = new JObject()
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("SetArrayOfPrimitiveValues", 0,
            new SetArrayOfPrimitiveValuesNodeConfiguration
            {
                TargetPath = "$.decimals",
                Values = new object[] { 1.1, 2.2, 3.3 }
            }, dataContext);

        var fn = A.Fake<NodeDelegate>();
        var testee = new SetArrayOfPrimitiveValuesNode(fn);
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        var decimals = dataContext.Current["decimals"] as JArray;
        Assert.NotNull(decimals);
        Assert.Equal(3, decimals.Count);
        Assert.Equal(1.1, decimals[0]?.Value<double>());
    }

    [Fact]
    public async Task ProcessObjectAsync_OverwritesExistingValue()
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = new JObject
            {
                ["data"] = new JArray { 100, 200 }
            }
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("SetArrayOfPrimitiveValues", 0,
            new SetArrayOfPrimitiveValuesNodeConfiguration
            {
                TargetPath = "$.data",
                Values = new object[] { 1, 2, 3 }
            }, dataContext);

        var fn = A.Fake<NodeDelegate>();
        var testee = new SetArrayOfPrimitiveValuesNode(fn);
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        var data = dataContext.Current["data"] as JArray;
        Assert.NotNull(data);
        Assert.Equal(3, data.Count);
        Assert.Equal(1, data[0]?.Value<int>());
    }

    [Fact]
    public async Task ProcessObjectAsync_WithNestedPath_SetsValues()
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = new JObject
            {
                ["nested"] = new JObject()
            }
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("SetArrayOfPrimitiveValues", 0,
            new SetArrayOfPrimitiveValuesNodeConfiguration
            {
                TargetPath = "$.nested.values",
                Values = new object[] { "a", "b", "c" }
            }, dataContext);

        var fn = A.Fake<NodeDelegate>();
        var testee = new SetArrayOfPrimitiveValuesNode(fn);
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        var nested = dataContext.Current["nested"] as JObject;
        Assert.NotNull(nested);
        var values = nested["values"] as JArray;
        Assert.NotNull(values);
        Assert.Equal(3, values.Count);
    }

    [Fact]
    public async Task ProcessObjectAsync_WithBooleanArray_SetsValues()
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = new JObject()
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("SetArrayOfPrimitiveValues", 0,
            new SetArrayOfPrimitiveValuesNodeConfiguration
            {
                TargetPath = "$.flags",
                Values = new object[] { true, false, true }
            }, dataContext);

        var fn = A.Fake<NodeDelegate>();
        var testee = new SetArrayOfPrimitiveValuesNode(fn);
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        var flags = dataContext.Current["flags"] as JArray;
        Assert.NotNull(flags);
        Assert.Equal(3, flags.Count);
        Assert.True(flags[0]?.Value<bool>());
        Assert.False(flags[1]?.Value<bool>());
    }
}
